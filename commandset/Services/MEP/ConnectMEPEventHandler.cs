using RevitMCPCommandSet.Models.MEP;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.MEP
{
  public class ConnectMEPEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<MEPConnectInfo> ConnectInfo { get; private set; }

    public AIResult<List<string>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<MEPConnectInfo> data)
    {
      ConnectInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var results = new List<string>();
        _warnings.Clear();

        foreach (var data in ConnectInfo)
        {
          using (Transaction transaction = new Transaction(doc, "Connect MEP"))
          {
            transaction.Start();

            Element elem1 = doc.GetElement(new ElementId(data.ElementId1));
            Element elem2 = doc.GetElement(new ElementId(data.ElementId2));

            if (elem1 == null || elem2 == null)
            {
              _warnings.Add($"Element(s) not found: {data.ElementId1}, {data.ElementId2}");
              transaction.Commit();
              continue;
            }

            // Try to get connectors from MEPCurve or FamilyInstance
            ConnectorSet connectors1 = GetConnectors(elem1);
            ConnectorSet connectors2 = GetConnectors(elem2);

            if (connectors1 == null || connectors1.Size == 0)
            {
              _warnings.Add($"No connectors found on element {data.ElementId1}");
              transaction.Commit();
              continue;
            }

            if (connectors2 == null || connectors2.Size == 0)
            {
              _warnings.Add($"No connectors found on element {data.ElementId2}");
              transaction.Commit();
              continue;
            }

            Connector conn1 = null;
            Connector conn2 = null;

            if (data.ConnectorIndex1 >= 0 && data.ConnectorIndex1 < connectors1.Size)
            {
              int i = 0;
              foreach (Connector c in connectors1)
              {
                if (i == data.ConnectorIndex1) { conn1 = c; break; }
                i++;
              }
            }

            if (data.ConnectorIndex2 >= 0 && data.ConnectorIndex2 < connectors2.Size)
            {
              int i = 0;
              foreach (Connector c in connectors2)
              {
                if (i == data.ConnectorIndex2) { conn2 = c; break; }
                i++;
              }
            }

            // Auto-select first connectors if not specified
            if (conn1 == null)
            {
              foreach (Connector c in connectors1) { conn1 = c; break; }
            }
            if (conn2 == null)
            {
              foreach (Connector c in connectors2) { conn2 = c; break; }
            }

            if (conn1 != null && conn2 != null)
            {
              switch (data.ConnectType.ToLower())
              {
                case "direct":
                  conn1.ConnectTo(conn2);
                  results.Add($"Connected {data.ElementId1} to {data.ElementId2}");
                  break;
                case "elbow":
                case "tee":
                case "reducer":
                case "cross":
                  conn1.ConnectTo(conn2);
                  results.Add($"Connected {data.ElementId1} to {data.ElementId2} ({data.ConnectType})");
                  break;
                default:
                  conn1.ConnectTo(conn2);
                  results.Add($"Connected {data.ElementId1} to {data.ElementId2}");
                  break;
              }
            }
            else
            {
              _warnings.Add($"Could not resolve connectors for {data.ElementId1} -> {data.ElementId2}");
            }

            transaction.Commit();
          }
        }

        string message = $"Successfully processed {results.Count} connection(s).";
        if (_warnings.Count > 0)
        {
          message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
        }
        Result = new AIResult<List<string>>
        {
          Success = true,
          Message = message,
          Response = results,
        };
      }
      catch (Exception ex)
      {
        Result = new AIResult<List<string>>
        {
          Success = false,
          Message = $"Error connecting MEP: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error connecting MEP: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    private ConnectorSet GetConnectors(Element elem)
    {
      ConnectorManager manager = null;

      if (elem is MEPCurve mepCurve)
      {
        manager = mepCurve.ConnectorManager;
      }
      else if (elem is FamilyInstance fi)
      {
        manager = fi.MEPModel?.ConnectorManager;
      }

      return manager?.Connectors;
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Connect MEP";
    }
  }
}
