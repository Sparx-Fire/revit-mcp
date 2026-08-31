using Autodesk.Revit.DB.Mechanical;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateSpaceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<SpaceCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<SpaceCreationInfo> data)
    {
      CreatedInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var elementIds = new List<int>();
        _warnings.Clear();

        foreach (var data in CreatedInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Create Space"))
          {
            transaction.Start();

            Level level = doc.FindNearestLevel(data.BaseLevel / 304.8);
            if (level == null)
            {
              _warnings.Add("No matching level found for space placement.");
              transaction.Commit();
              continue;
            }

            XYZ point = JZPoint.ToXYZ(data.Location);
            Space space = VersionCompat.CreateSpace(doc, level.Id, point);

            if (space != null)
            {
              if (!string.IsNullOrEmpty(data.Name))
              {
                Parameter nameParam = space.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (nameParam != null)
                  nameParam.Set(data.Name);
              }

              if (!string.IsNullOrEmpty(data.Number))
              {
                Parameter numberParam = space.get_Parameter(BuiltInParameter.ROOM_NUMBER);
                if (numberParam != null)
                  numberParam.Set(data.Number);
              }

              if (!string.IsNullOrEmpty(data.SpaceType))
              {
                Parameter typeParam = space.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT);
                if (typeParam != null)
                  typeParam.Set(data.SpaceType);
              }

              elementIds.Add(space.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} space(s).";
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
          Message = $"Error creating space: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating space: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Create Space";
    }
  }
}
