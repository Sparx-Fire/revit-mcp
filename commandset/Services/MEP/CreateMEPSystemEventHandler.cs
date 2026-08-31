using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class CreateMEPSystemEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<MEPSystemCreationInfo> CreatedInfo { get; private set; }

    public AIResult<List<int>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<MEPSystemCreationInfo> data)
    {
      CreatedInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var systemIds = new List<int>();
        _warnings.Clear();

        foreach (var data in CreatedInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Create MEP System"))
          {
            transaction.Start();

            ElementId systemTypeId = GetSystemTypeId(data.SystemType);
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
            {
              _warnings.Add($"Unsupported system type: {data.SystemType}");
              transaction.Commit();
              continue;
            }

            MEPSystem system = null;

            // Mechanical systems
            if (data.SystemType.Equals("SupplyAir", StringComparison.OrdinalIgnoreCase) ||
                data.SystemType.Equals("ReturnAir", StringComparison.OrdinalIgnoreCase) ||
                data.SystemType.Equals("ExhaustAir", StringComparison.OrdinalIgnoreCase))
            {
              system = MechanicalSystem.Create(doc, systemTypeId);
            }
            // Plumbing systems
            else if (data.SystemType.Equals("Sanitary", StringComparison.OrdinalIgnoreCase) ||
                     data.SystemType.Equals("HydronicSupply", StringComparison.OrdinalIgnoreCase) ||
                     data.SystemType.Equals("HydronicReturn", StringComparison.OrdinalIgnoreCase))
            {
              system = PipingSystem.Create(doc, systemTypeId);
            }
            else
            {
              _warnings.Add($"Unhandled system type: {data.SystemType}");
            }

            if (system != null)
            {
              if (!string.IsNullOrEmpty(data.Name))
              {
                system.Name = data.Name;
              }

              if (data.ElementIds != null && data.ElementIds.Count > 0)
              {
                List<ElementId> elemIds = data.ElementIds.Select(id => new ElementId(id)).ToList();
                VersionCompat.AddElementsToMEPSystem(system, elemIds);
              }

              systemIds.Add(system.Id.GetIntValue());
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully created {systemIds.Count} MEP system(s).";
        if (_warnings.Count > 0)
        {
          message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
        }
        Result = new AIResult<List<int>>
        {
          Success = true,
          Message = message,
          Response = systemIds,
        };
      }
      catch (Exception ex)
      {
        Result = new AIResult<List<int>>
        {
          Success = false,
          Message = $"Error creating MEP system: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error creating MEP system: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    private ElementId GetSystemTypeId(string systemType)
    {
      // ================================================================
      // Mechanical system types — use MEPSystemClassification (language-agnostic)
      // ================================================================
      var mechClassMap = new Dictionary<string, MEPSystemClassification>(StringComparer.OrdinalIgnoreCase)
      {
        ["supplyair"] = MEPSystemClassification.SupplyAir,
        ["returnair"] = MEPSystemClassification.ReturnAir,
        ["exhaustair"] = MEPSystemClassification.ExhaustAir,
      };

      if (mechClassMap.TryGetValue(systemType, out var mechClass))
      {
        var type = new FilteredElementCollector(doc)
            .OfClass(typeof(MechanicalSystemType))
            .Cast<MechanicalSystemType>()
            .FirstOrDefault(st => st.SystemClassification == mechClass);
        return type?.Id ?? ElementId.InvalidElementId;
      }

      // ================================================================
      // Piping system types — R26 removed PipingSystemClassification enum AND
      // RBS_PIPING_SYSTEM_CLASSIFICATION_PARAM, so use dual-language name matching
      // ================================================================
      var pipeNames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
      {
        ["sanitary"] = new[] { "Sanitary", "卫生设备" },
        ["hydronicsupply"] = new[] { "Hydronic Supply", "HydronicSupply", "循环供水" },
        ["hydronicreturn"] = new[] { "Hydronic Return", "HydronicReturn", "循环回水" },
      };

      if (pipeNames.TryGetValue(systemType, out var altNames))
      {
        foreach (var name in altNames)
        {
          var type = new FilteredElementCollector(doc)
              .OfClass(typeof(PipingSystemType))
              .Cast<PipingSystemType>()
              .FirstOrDefault(st => st.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
          if (type != null) return type.Id;
        }
        return ElementId.InvalidElementId;
      }

      // ================================================================
      // Fallback: try matching by name (for custom system types)
      // ================================================================
      var allMech = new FilteredElementCollector(doc)
          .OfClass(typeof(MechanicalSystemType))
          .Cast<MechanicalSystemType>()
          .FirstOrDefault(st => st.Name.Equals(systemType, StringComparison.OrdinalIgnoreCase));
      if (allMech != null) return allMech.Id;

      var allPipe = new FilteredElementCollector(doc)
          .OfClass(typeof(PipingSystemType))
          .Cast<PipingSystemType>()
          .FirstOrDefault(st => st.Name.Equals(systemType, StringComparison.OrdinalIgnoreCase));
      if (allPipe != null) return allPipe.Id;

      return ElementId.InvalidElementId;
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Create MEP System";
    }
  }
}
