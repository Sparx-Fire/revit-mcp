using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services;

public class GetDocumentStylesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public DocumentStylesResult ResultInfo { get; private set; } = new();
    public bool TaskCompleted { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = app.ActiveUIDocument?.Document
                ?? throw new InvalidOperationException("No active Revit document.");

            ResultInfo = new DocumentStylesResult
            {
                Success = true,
                Message = "Document styles collected successfully.",
                DimensionTypes = CollectDimensionTypes(doc),
                GridTypes = CollectGridTypes(doc),
                TextNoteTypes = CollectTextNoteTypes(doc),
                LinePatterns = CollectLinePatterns(doc),
                GraphicsStyles = CollectGraphicsStyles(doc),
                TitleBlocks = CollectTitleBlocks(doc)
            };
        }
        catch (Exception ex)
        {
            ResultInfo = new DocumentStylesResult
            {
                Success = false,
                Message = $"Failed to collect document styles: {ex.Message}"
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    public string GetName() => "Get Document Styles";

    private static List<DimensionTypeStyleInfo> CollectDimensionTypes(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .OrderBy(type => type.Name)
            .Select(type => new DimensionTypeStyleInfo
            {
                Id = type.Id.GetValue(),
                UniqueId = type.UniqueId,
                Name = type.Name,
                StyleType = type.StyleType.ToString()
            })
            .ToList();
    }

    private static List<NamedStyleInfo> CollectGridTypes(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(GridType))
            .Cast<GridType>()
            .OrderBy(type => type.Name)
            .Select(type => new NamedStyleInfo
            {
                Id = type.Id.GetValue(),
                UniqueId = type.UniqueId,
                Name = type.Name
            })
            .ToList();
    }

    private static List<TextNoteTypeStyleInfo> CollectTextNoteTypes(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(TextNoteType))
            .Cast<TextNoteType>()
            .OrderBy(type => type.Name)
            .Select(type =>
            {
                double? textHeightMm = null;
                var textSizeParam = type.get_Parameter(BuiltInParameter.TEXT_SIZE);
                if (textSizeParam != null && textSizeParam.HasValue)
                    textHeightMm = RevitUnitConversion.ToMillimeters(textSizeParam.AsDouble());

                var fontParam = type.get_Parameter(BuiltInParameter.TEXT_FONT);
                var font = fontParam?.AsString() ?? string.Empty;

                return new TextNoteTypeStyleInfo
                {
                    Id = type.Id.GetValue(),
                    UniqueId = type.UniqueId,
                    Name = type.Name,
                    TextHeightMm = textHeightMm,
                    Font = font
                };
            })
            .ToList();
    }

    private static List<NamedStyleInfo> CollectLinePatterns(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(LinePatternElement))
            .Cast<LinePatternElement>()
            .OrderBy(pattern => pattern.Name)
            .Select(pattern => new NamedStyleInfo
            {
                Id = pattern.Id.GetValue(),
                UniqueId = pattern.UniqueId,
                Name = pattern.Name
            })
            .ToList();
    }

    private static List<GraphicsStyleInfo> CollectGraphicsStyles(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(GraphicsStyle))
            .Cast<GraphicsStyle>()
            .Where(style => !string.IsNullOrWhiteSpace(style.Name))
            .OrderBy(style => style.Name)
            .Select(style => new GraphicsStyleInfo
            {
                Id = style.Id.GetValue(),
                UniqueId = style.UniqueId,
                Name = style.Name,
                GraphicsStyleType = style.GraphicsStyleType.ToString(),
                Category = style.GraphicsStyleCategory?.Name ?? string.Empty
            })
            .ToList();
    }

    private static List<TitleBlockStyleInfo> CollectTitleBlocks(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .OrderBy(symbol => symbol.FamilyName)
            .ThenBy(symbol => symbol.Name)
            .Select(symbol => new TitleBlockStyleInfo
            {
                Id = symbol.Id.GetValue(),
                UniqueId = symbol.UniqueId,
                FamilyName = symbol.FamilyName,
                TypeName = symbol.Name
            })
            .ToList();
    }
}
