using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateSweptShapeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<SweptShapeCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<SweptShapeCreationInfo> data)
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
          using (Transaction transaction = new Transaction(doc, "Create Swept Shape"))
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

            // Build sweep path from pathPoints
            if (data.PathPoints.Count < 2)
            {
              _warnings.Add("Swept shape requires at least 2 path points.");
              transaction.Commit();
              continue;
            }

            CurveLoop pathLoop = new CurveLoop();
            for (int i = 0; i < data.PathPoints.Count - 1; i++)
            {
              XYZ p1 = JZPoint.ToXYZ(data.PathPoints[i]);
              XYZ p2 = JZPoint.ToXYZ(data.PathPoints[i + 1]);
              pathLoop.Append(Line.CreateBound(p1, p2));
            }

            // Calculate path start direction for profile rotation
            XYZ pathStartDir = XYZ.BasisZ;
            if (pathLoop.Any())
            {
              var firstCurve = pathLoop.First();
              pathStartDir = (firstCurve.GetEndPoint(1) - firstCurve.GetEndPoint(0)).Normalize();
            }

            // Build section profile (initially in XY plane at origin)
            CurveLoop sectionLoop = new CurveLoop();
            switch (data.SectionType.ToLower())
            {
              case "rect":
              {
                double w = (data.Width / 2) / 304.8;
                double h = (data.Height / 2) / 304.8;
                sectionLoop.Append(Line.CreateBound(new XYZ(-w, -h, 0), new XYZ(w, -h, 0)));
                sectionLoop.Append(Line.CreateBound(new XYZ(w, -h, 0), new XYZ(w, h, 0)));
                sectionLoop.Append(Line.CreateBound(new XYZ(w, h, 0), new XYZ(-w, h, 0)));
                sectionLoop.Append(Line.CreateBound(new XYZ(-w, h, 0), new XYZ(-w, -h, 0)));
                break;
              }
              case "circle":
              {
                double r = data.Radius / 304.8;
                sectionLoop.Append(Arc.Create(XYZ.Zero, r, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY));
                break;
              }
              case "horseshoe":
              {
                double r = data.Radius / 304.8;
                double w = (data.Width / 2) / 304.8;
                double h = (data.Height / 2) / 304.8;
                // Top arc
                sectionLoop.Append(Arc.Create(new XYZ(-w, 0, 0), new XYZ(w, 0, 0), new XYZ(0, h, 0)));
                // Right vertical
                sectionLoop.Append(Line.CreateBound(new XYZ(w, 0, 0), new XYZ(w, -h, 0)));
                // Bottom
                sectionLoop.Append(Line.CreateBound(new XYZ(w, -h, 0), new XYZ(-w, -h, 0)));
                // Left vertical
                sectionLoop.Append(Line.CreateBound(new XYZ(-w, -h, 0), new XYZ(-w, 0, 0)));
                break;
              }
              default:
                _warnings.Add($"Unsupported section type: {data.SectionType}");
                break;
            }

            // Rotate profile so its normal aligns with path direction
            if (sectionLoop.Any() && pathStartDir.DotProduct(XYZ.BasisZ) < 0.999)
            {
              // Profile is in XY plane (normal = Z). Rotate to align with path direction.
              Transform rotation = Transform.CreateRotation(XYZ.BasisY, Math.Acos(pathStartDir.DotProduct(XYZ.BasisZ)));
              // Refine: use proper alignment transform
              XYZ profileNormal = XYZ.BasisZ;
              Transform alignTransform = Transform.Identity;
              if (!pathStartDir.IsAlmostEqualTo(profileNormal))
              {
                XYZ axis = profileNormal.CrossProduct(pathStartDir).Normalize();
                double angle = profileNormal.AngleTo(pathStartDir);
                if (angle > 1e-9)
                  alignTransform = Transform.CreateRotation(axis, angle);
              }
              CurveLoop rotatedSection = new CurveLoop();
              foreach (Curve c in sectionLoop)
              {
                if (c is Line line)
                {
                  rotatedSection.Append(Line.CreateBound(
                      alignTransform.OfPoint(line.GetEndPoint(0)),
                      alignTransform.OfPoint(line.GetEndPoint(1))));
                }
                else if (c is Arc arc)
                {
                  rotatedSection.Append(Arc.Create(
                      alignTransform.OfPoint(arc.GetEndPoint(0)),
                      alignTransform.OfPoint(arc.GetEndPoint(1)),
                      alignTransform.OfPoint(arc.Center)));
                }
                else
                {
                  rotatedSection.Append(c.CreateTransformed(alignTransform));
                }
              }
              sectionLoop = rotatedSection;
            }

            if (sectionLoop.Any() && pathLoop.Any())
            {
              Solid swept = GeometryCreationUtilities.CreateSweptGeometry(pathLoop, 0, 0, new List<CurveLoop> { sectionLoop });
              ds.SetShape(new List<GeometryObject> { swept });
              elementIds.Add(ds.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} swept shape(s).";
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
          Message = $"Error creating swept shape: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating swept shape: {ex.Message}");
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
      return "Create Swept Shape";
    }
  }
}
