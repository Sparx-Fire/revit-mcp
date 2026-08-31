using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateDirectShapeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<DirectShapeCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<DirectShapeCreationInfo> data)
    {
      CreatedInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        if (doc.IsFamilyDocument)
        {
          Result = new AIResult<List<int>>
          {
            Success = false,
            Message = "DirectShape is not supported in Family Documents. Please open a Project Document to use this tool.",
            Response = new List<int>()
          };
          return;
        }

        var elementIds = new List<int>();
        _warnings.Clear();

        foreach (var data in CreatedInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Create Direct Shape"))
          {
            transaction.Start();

            Category category = doc.Settings.Categories.get_Item(data.Category);
            if (category == null)
            {
              _warnings.Add($"Category '{data.Category}' not found. Using default.");
              category = doc.Settings.Categories.get_Item("Generic Models");
            }

            ElementId catId = category.Id;
            DirectShape ds = DirectShape.CreateElement(doc, catId);

            List<GeometryObject> geometryObjects = new List<GeometryObject>();

            switch (data.ShapeType.ToLower())
            {
              case "box":
              {
                XYZ center = JZPoint.ToXYZ(data.Center);
                double w = data.Width / 304.8;
                double d = data.Depth / 304.8;
                double h = data.Height / 304.8;
                XYZ profile0 = new XYZ(-w / 2, -d / 2, 0);
                XYZ profile1 = new XYZ(w / 2, -d / 2, 0);
                XYZ profile2 = new XYZ(w / 2, d / 2, 0);
                XYZ profile3 = new XYZ(-w / 2, d / 2, 0);

                CurveLoop baseLoop = new CurveLoop();
                baseLoop.Append(Line.CreateBound(profile0, profile1));
                baseLoop.Append(Line.CreateBound(profile1, profile2));
                baseLoop.Append(Line.CreateBound(profile2, profile3));
                baseLoop.Append(Line.CreateBound(profile3, profile0));

                List<CurveLoop> loops = new List<CurveLoop> { baseLoop };
                Solid box = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, h);
                geometryObjects.Add(box);
                break;
              }
              case "cylinder":
              {
                double r = data.Radius / 304.8;
                double h = data.Height / 304.8;

                CurveLoop circleLoop = new CurveLoop();
                circleLoop.Append(Arc.Create(XYZ.Zero, r, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY));

                List<CurveLoop> loops = new List<CurveLoop> { circleLoop };
                Solid cylinder = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, h);
                geometryObjects.Add(cylinder);
                break;
              }
              case "extrusion":
              {
                if (data.Points.Count < 2)
                {
                  _warnings.Add("Extrusion requires at least 2 profile points.");
                  break;
                }

                CurveLoop profileLoop = new CurveLoop();
                for (int i = 0; i < data.Points.Count; i++)
                {
                  XYZ p1 = JZPoint.ToXYZ(data.Points[i]);
                  XYZ p2 = JZPoint.ToXYZ(data.Points[(i + 1) % data.Points.Count]);
                  profileLoop.Append(Line.CreateBound(p1, p2));
                }

                double len = data.ExtrusionLength / 304.8;
                XYZ dir = new XYZ(
                    data.ExtrusionDir.X / 304.8,
                    data.ExtrusionDir.Y / 304.8,
                    data.ExtrusionDir.Z / 304.8);
                if (dir.GetLength() < 1e-9)
                  dir = XYZ.BasisZ;

                List<CurveLoop> loops = new List<CurveLoop> { profileLoop };
                Solid extrusion = GeometryCreationUtilities.CreateExtrusionGeometry(loops, dir.Normalize(), len);
                geometryObjects.Add(extrusion);
                break;
              }
              default:
                _warnings.Add($"Unsupported shape type: {data.ShapeType}");
                break;
            }

            if (geometryObjects.Count > 0)
            {
              ds.SetShape(geometryObjects);

              if (!string.IsNullOrEmpty(data.Material))
              {
                FilteredElementCollector matCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(Material));
                foreach (Material mat in matCollector)
                {
                  if (mat.Name.Equals(data.Material, StringComparison.OrdinalIgnoreCase))
                  {
                    ds.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM)?.Set(mat.Id);
                    break;
                  }
                }
              }

              elementIds.Add(ds.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} direct shape(s).";
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
          Message = $"Error creating direct shape: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating direct shape: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 30000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Create Direct Shape";
    }
  }
}
