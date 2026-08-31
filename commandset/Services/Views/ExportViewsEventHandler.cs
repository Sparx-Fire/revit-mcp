using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
  public class ExportViewsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
  {
    private UIApplication uiApp;
    private UIDocument uiDoc => uiApp.ActiveUIDocument;
    private Document doc => uiDoc.Document;
    private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;

    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public List<ExportSettingsInfo> ExportInfo { get; private set; }

    public AIResult<List<string>> Result { get; private set; }
    private List<string> _warnings = new List<string>();

    public void SetParameters(List<ExportSettingsInfo> data)
    {
      ExportInfo = data;
      _resetEvent.Reset();
    }

    public void Execute(UIApplication uiapp)
    {
      uiApp = uiapp;

      try
      {
        var exportedFiles = new List<string>();
        _warnings.Clear();

        foreach (var data in ExportInfo)
        {
          if (data.ViewIds == null || data.ViewIds.Count == 0)
          {
            _warnings.Add("No view IDs provided for export.");
            continue;
          }

          foreach (int viewId in data.ViewIds)
          {
            ElementId elemId = new ElementId(viewId);
            View view = doc.GetElement(elemId) as View;

            if (view == null)
            {
              _warnings.Add($"View with ID {viewId} not found.");
              continue;
            }

            string folderPath = data.FolderPath;
            string fileName = data.FileName;

            if (string.IsNullOrEmpty(fileName))
              fileName = view.Name;

            switch (data.Format.ToUpper())
            {
              case "PNG":
              case "JPG":
              {
                ImageExportOptions imgOpts = new ImageExportOptions();
                imgOpts.FilePath = System.IO.Path.Combine(folderPath, fileName);
                imgOpts.ZoomType = ZoomFitType.FitToPage;
                imgOpts.PixelSize = 1024;
                imgOpts.ImageResolution = ImageResolution.DPI_150;
#if REVIT2026_OR_GREATER
                // R26: HLRScale and Format removed from ImageExportOptions
#elif REVIT2025_OR_GREATER
                imgOpts.HLRScale = false;
#endif
                imgOpts.ExportRange = ExportRange.SetOfViews;
                imgOpts.SetViewsAndSheets(new List<ElementId> { elemId });
#if REVIT2026_OR_GREATER
                // R26: Format removed from ImageExportOptions
#elif REVIT2025_OR_GREATER
                imgOpts.Format = data.Format.ToUpper() == "PNG"
                    ? ImageFileType.PNG
                    : ImageFileType.JPEGLossless;
#endif

                doc.ExportImage(imgOpts);
                exportedFiles.Add($"{fileName}.{data.Format.ToLower()}");
                break;
              }
              case "DWG":
              case "DXF":
              {
                DWGExportOptions dwgOpts = new DWGExportOptions();
#if REVIT2026_OR_GREATER
                // R26: ExportLayerTable removed from DWGExportOptions
#elif REVIT2025_OR_GREATER
                dwgOpts.ExportLayerTable = false;
#endif

                ICollection<ElementId> viewIds = new List<ElementId> { elemId };
                doc.Export(folderPath, fileName, viewIds, dwgOpts);
                exportedFiles.Add($"{fileName}.{data.Format.ToLower()}");
                break;
              }
              case "IFC":
              {
#if REVIT2026_OR_GREATER
                // R26: IFC export via different method
                _warnings.Add("IFC export not supported in Revit 2026 via this API");
#elif REVIT2025_OR_GREATER
                IFCExportOptions ifcOpts = new IFCExportOptions();

                ICollection<ElementId> viewIds = new List<ElementId> { elemId };
                doc.Export(folderPath, fileName, viewIds, ifcOpts);
#else
                SATExportOptions satOpts = new SATExportOptions();

                ICollection<ElementId> viewIds = new List<ElementId> { elemId };
                doc.Export(folderPath, fileName, viewIds, satOpts);
#endif
                exportedFiles.Add($"{fileName}.ifc");
                break;
              }
              case "DGN":
              {
                DGNExportOptions dgnOpts = new DGNExportOptions();

                ICollection<ElementId> viewIds = new List<ElementId> { elemId };
                doc.Export(folderPath, fileName, viewIds, dgnOpts);
                exportedFiles.Add($"{fileName}.dgn");
                break;
              }
              default:
                _warnings.Add($"Unsupported export format: {data.Format}");
                break;
            }
          }
        }

        string message = $"Successfully exported {exportedFiles.Count} file(s).";
        if (_warnings.Count > 0)
        {
          message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
        }
        Result = new AIResult<List<string>>
        {
          Success = true,
          Message = message,
          Response = exportedFiles,
        };
      }
      catch (Exception ex)
      {
        Result = new AIResult<List<string>>
        {
          Success = false,
          Message = $"Error exporting views: {ex.Message}",
        };
        TaskDialog.Show("Error", $"Error exporting views: {ex.Message}");
      }
      finally
      {
        _resetEvent.Set();
      }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 60000)
    {
      _resetEvent.Reset();
      return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName()
    {
      return "Export Views";
    }
  }
}
