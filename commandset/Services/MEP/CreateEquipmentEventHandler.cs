using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateEquipmentEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<EquipmentCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<EquipmentCreationInfo> data)
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

          FamilySymbol symbol = null;
          if (data.TypeId != -1 && data.TypeId != 0)
          {
            ElementId typeEleId = new ElementId(data.TypeId);
            if (typeEleId != null)
            {
              Element typeEle = doc.GetElement(typeEleId);
              if (typeEle != null && typeEle is FamilySymbol)
              {
                symbol = typeEle as FamilySymbol;
              }
            }
          }

          if (symbol == null)
          {
            string categoryName = string.IsNullOrEmpty(data.Category) ? "Mechanical Equipment" : data.Category;

            if (!string.IsNullOrEmpty(data.FamilyName))
            {
              symbol = new FilteredElementCollector(doc)
                  .OfClass(typeof(FamilySymbol))
                  .Cast<FamilySymbol>()
                  .FirstOrDefault(fs =>
                      fs.FamilyName.Equals(data.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                      (string.IsNullOrEmpty(data.EquipmentType) ||
                       fs.Name.Equals(data.EquipmentType, StringComparison.OrdinalIgnoreCase)));
            }

            if (symbol == null)
            {
              symbol = new FilteredElementCollector(doc)
                  .OfClass(typeof(FamilySymbol))
                  .Cast<FamilySymbol>()
                  .FirstOrDefault(fs => fs.IsActive);
            }

            if (symbol == null)
            {
              _warnings.Add("No family symbols available in project.");
              continue;
            }
            if (requestedTypeId != -1 && requestedTypeId != 0)
            {
              _warnings.Add($"Requested typeId {requestedTypeId} not found. Defaulted to '{symbol.FamilyName}: {symbol.Name}' (ID: {symbol.Id.GetValue()})");
            }
          }

          using (Transaction transaction = new Transaction(doc, "Create Equipment"))
          {
            transaction.Start();

            if (!symbol.IsActive)
              symbol.Activate();

            XYZ location = JZPoint.ToXYZ(data.Location);
            FamilyInstance instance = doc.Create.NewFamilyInstance(
                location,
                symbol,
                baseLevel,
                Autodesk.Revit.DB.Structure.StructuralType.NonStructural
            );

            if (instance != null)
            {
              if (data.Rotation != 0)
              {
                XYZ origin = JZPoint.ToXYZ(data.Location);
                Line rotationAxis = Line.CreateBound(origin, origin + XYZ.BasisZ);
                double angleRadians = data.Rotation * Math.PI / 180.0;
                ElementTransformUtils.RotateElement(doc, instance.Id, rotationAxis, angleRadians);
              }

              Parameter offsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
              if (offsetParam != null)
                offsetParam.Set(baseOffset);

              elementIds.Add(instance.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {elementIds.Count} equipment instance(s).";
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
          Message = $"Error creating equipment: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating equipment: {ex.Message}");
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
      return "Create Equipment";
    }
  }
}
