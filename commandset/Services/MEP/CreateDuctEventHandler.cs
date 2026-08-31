using Autodesk.Revit.DB.Mechanical;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateDuctEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<DuctCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<DuctCreationInfo> data)
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

          DuctType ductType = null;
          if (data.TypeId != -1 && data.TypeId != 0)
          {
            ElementId typeEleId = new ElementId(data.TypeId);
            if (typeEleId != null)
            {
              Element typeEle = doc.GetElement(typeEleId);
              if (typeEle != null && typeEle is DuctType)
              {
                ductType = typeEle as DuctType;
              }
            }
          }

          if (ductType == null)
          {
            ductType = new FilteredElementCollector(doc)
                .OfClass(typeof(DuctType))
                .Cast<DuctType>()
                .FirstOrDefault(d => d.Shape == ConnectorProfileType.Rectangular);

            if (ductType == null)
            {
              _warnings.Add("No duct types available in project.");
              continue;
            }
            if (requestedTypeId != -1 && requestedTypeId != 0)
            {
              _warnings.Add($"Requested duct typeId {requestedTypeId} not found. Defaulted to '{ductType.Name}' (ID: {ductType.Id.GetValue()})");
            }
          }

          using (Transaction transaction = new Transaction(doc, "Create Duct"))
          {
            transaction.Start();

            MEPSystemType mepSystemType = new FilteredElementCollector(doc)
                .OfClass(typeof(MEPSystemType))
                .Cast<MEPSystemType>()
                .FirstOrDefault(m => m.SystemClassification == MEPSystemClassification.SupplyAir);

            if (mepSystemType != null)
            {
              Duct duct = Duct.Create(
                  doc,
                  mepSystemType.Id,
                  ductType.Id,
                  baseLevel.Id,
                  JZPoint.ToXYZ(data.StartPoint),
                  JZPoint.ToXYZ(data.EndPoint)
              );

              if (duct != null)
              {
                Parameter offsetParam = duct.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                if (offsetParam != null)
                  offsetParam.Set(baseOffset);
                elementIds.Add(duct.Id.GetIntValue());
              }
            }
            else
            {
              _warnings.Add("No MEPSystemType with Supply Air classification found. Duct not created.");
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} duct(s).";
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
          Message = $"Error creating duct: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating duct: {ex.Message}");
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
      return "Create Duct";
    }
  }
}
