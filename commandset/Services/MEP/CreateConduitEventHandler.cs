using Autodesk.Revit.DB.Electrical;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateConduitEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<ConduitCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<ConduitCreationInfo> data)
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
          int requestedTypeId = data.TypeId;

          Level baseLevel = doc.FindNearestLevel(data.BaseLevel / 304.8);
          double baseOffset = (data.BaseOffset + data.BaseLevel) / 304.8 - baseLevel.Elevation;
          if (baseLevel == null)
            continue;

          ConduitType conduitType = null;
          if (data.TypeId != -1 && data.TypeId != 0)
          {
            ElementId typeEleId = new ElementId(data.TypeId);
            if (typeEleId != null)
            {
              Element typeEle = doc.GetElement(typeEleId);
              if (typeEle != null && typeEle is ConduitType)
              {
                conduitType = typeEle as ConduitType;
              }
            }
          }

          if (conduitType == null)
          {
            conduitType = new FilteredElementCollector(doc)
                .OfClass(typeof(ConduitType))
                .Cast<ConduitType>()
                .FirstOrDefault();

            if (conduitType == null)
            {
              _warnings.Add("No conduit types available in project.");
              continue;
            }
            if (requestedTypeId != -1 && requestedTypeId != 0)
            {
              _warnings.Add($"Requested conduit typeId {requestedTypeId} not found. Defaulted to '{conduitType.Name}' (ID: {conduitType.Id.GetValue()})");
            }
          }

          using (Transaction transaction = new Transaction(doc, "Create Conduit"))
          {
            transaction.Start();

            Conduit conduit = Conduit.Create(
                doc,
                conduitType.Id,
                JZPoint.ToXYZ(data.StartPoint),
                JZPoint.ToXYZ(data.EndPoint),
                baseLevel.Id
            );

            if (conduit != null)
            {
              Parameter offsetParam = conduit.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
              if (offsetParam != null)
                offsetParam.Set(baseOffset);
              elementIds.Add(conduit.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} conduit(s).";
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
          Message = $"Error creating conduit: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating conduit: {ex.Message}");
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
      return "Create Conduit";
    }
  }
}
