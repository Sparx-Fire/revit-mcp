using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class PlaceFamilyInstanceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<FamilyInstancePlacementInfo> PlacementInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<FamilyInstancePlacementInfo> data)
    {
      PlacementInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var elementIds = new List<int>();
        _warnings.Clear();

        foreach (var data in PlacementInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Place Family Instance"))
          {
            transaction.Start();

            Element symbolElem = doc.GetElement(new ElementId(data.SymbolId));
            if (symbolElem == null || !(symbolElem is FamilySymbol symbol))
            {
              _warnings.Add($"FamilySymbol with ID {data.SymbolId} not found.");
              transaction.Commit();
              continue;
            }

            if (!symbol.IsActive)
            {
              symbol.Activate();
            }

            XYZ point = JZPoint.ToXYZ(data.Location);
            FamilyInstance instance = null;

            switch (data.PlacementType.ToLower())
            {
              case "unhosted":
              {
                Level level = doc.FindNearestLevel(data.Level / 304.8);
                if (level == null)
                {
                  _warnings.Add("No matching level for unhosted placement.");
                  break;
                }
                instance = doc.Create.NewFamilyInstance(point, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                break;
              }
              case "hosted":
              {
                Element hostElem = doc.GetElement(new ElementId(data.HostId));
                if (hostElem == null)
                {
                  _warnings.Add($"Host element {data.HostId} not found.");
                  break;
                }
                instance = doc.Create.NewFamilyInstance(point, symbol, hostElem, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                break;
              }
              case "facebased":
              {
                Element hostElem = doc.GetElement(new ElementId(data.HostId));
                if (hostElem != null)
                {
                  Reference faceRef = FindClosestFace(hostElem, point);
                  if (faceRef != null)
                    instance = doc.Create.NewFamilyInstance(faceRef, point, XYZ.BasisX, symbol);
                }
                else
                {
                  instance = doc.Create.NewFamilyInstance(point, symbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                }
                break;
              }
              case "workplane":
              {
                SketchPlane skp = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, point));
                instance = doc.Create.NewFamilyInstance(point, symbol, skp, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                break;
              }
              default:
              {
                Level level = doc.FindNearestLevel(data.Level / 304.8);
                if (level == null)
                  level = doc.FindNearestLevel(0);
                instance = doc.Create.NewFamilyInstance(point, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                break;
              }
            }

            if (instance != null)
            {
              if (data.Rotation != 0)
              {
                double angle = data.Rotation * Math.PI / 180.0;
                Line axis = Line.CreateBound(point, new XYZ(point.X, point.Y, point.Z + 1));
                ElementTransformUtils.RotateElement(doc, instance.Id, axis, angle);
              }

              elementIds.Add(instance.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully placed {elementIds.Count} family instance(s).";
        if (_warnings.Count > 0)
        {
          message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
        }
        Result = new AIResult<List<int>>
        {
          Success = true,
          Message = message,
          Response = elementIds,
        };
      }
      catch (Exception ex)
      {
        Result = new AIResult<List<int>>
        {
          Success = false,
          Message = $"Error placing family instance: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error placing family instance: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    private Reference FindClosestFace(Element elem, XYZ point)
    {
      Options geomOpts = new Options();
      GeometryElement geom = elem.get_Geometry(geomOpts);
      if (geom == null) return null;

      double closestDist = double.MaxValue;
      Reference closestRef = null;

      foreach (GeometryObject geomObj in geom)
      {
        Solid solid = geomObj as Solid;
        if (solid == null) continue;

        foreach (Face face in solid.Faces)
        {
          IntersectionResult ir = face.Project(point);
          if (ir != null && ir.Distance < closestDist)
          {
            closestDist = ir.Distance;
            closestRef = VersionCompat.GetIntersectionReference(ir);
          }
        }
      }

      return closestRef;
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Place Family Instance";
    }
  }
}
