using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views;

public class CreateSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private SheetCreationInfo _sheetInfo;
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public SheetCreationResult ResultInfo { get; private set; } = new SheetCreationResult();
    public bool TaskCompleted { get; private set; }

    public void SetParameters(SheetCreationInfo sheetInfo)
    {
        _sheetInfo = sheetInfo ?? throw new ArgumentNullException(nameof(sheetInfo));
        TaskCompleted = false;
        _resetEvent.Reset();
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        var warnings = new List<string>();

        try
        {
            var doc = app.ActiveUIDocument.Document;
            ViewSheet sheet;
            FamilySymbol titleBlock;

            using (var tx = new Transaction(doc, "Create Sheet"))
            {
                tx.Start();

                titleBlock = ResolveTitleBlock(doc, _sheetInfo, warnings);
                sheet = ViewSheet.Create(doc, titleBlock.Id);

                if (!string.IsNullOrWhiteSpace(_sheetInfo.SheetNumber))
                    sheet.SheetNumber = GetUniqueSheetNumber(doc, _sheetInfo.SheetNumber.Trim());

                if (!string.IsNullOrWhiteSpace(_sheetInfo.SheetName))
                    sheet.Name = _sheetInfo.SheetName.Trim();

                ApplyRevisions(doc, sheet, _sheetInfo, warnings);

                tx.Commit();
            }

            ResultInfo = new SheetCreationResult
            {
                Success = true,
                Message = $"Successfully created sheet '{sheet.SheetNumber}'",
                SheetId = GetElementIdValue(sheet.Id),
                SheetUniqueId = sheet.UniqueId,
                SheetNumber = sheet.SheetNumber,
                SheetName = sheet.Name,
                TitleBlockTypeId = GetElementIdValue(titleBlock.Id),
                TitleBlockFamilyName = titleBlock.FamilyName,
                TitleBlockTypeName = titleBlock.Name,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            ResultInfo = new SheetCreationResult
            {
                Success = false,
                Message = $"Error creating sheet: {ex.Message}",
                Warnings = warnings
            };
        }
        finally
        {
            TaskCompleted = true;
            _resetEvent.Set();
        }
    }

    private static FamilySymbol ResolveTitleBlock(
        Document doc,
        SheetCreationInfo info,
        List<string> warnings)
    {
        if (info.TitleBlockTypeId > 0)
        {
            var symbol = doc.GetElement(new ElementId(info.TitleBlockTypeId)) as FamilySymbol;
            if (symbol != null && symbol.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks)
                return EnsureTitleBlockActive(doc, symbol);

            warnings.Add($"Title block type id '{info.TitleBlockTypeId}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(info.TitleBlockFamilyName) ||
            !string.IsNullOrWhiteSpace(info.TitleBlockTypeName))
        {
            var symbol = FindTitleBlockByName(doc, info.TitleBlockFamilyName, info.TitleBlockTypeName);
            if (symbol != null)
                return EnsureTitleBlockActive(doc, symbol);

            warnings.Add(
                $"Title block '{info.TitleBlockFamilyName}' / '{info.TitleBlockTypeName}' was not found.");
        }

        var fallback = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault();

        if (fallback == null)
            throw new InvalidOperationException("No title block types are loaded in the project.");

        warnings.Add($"Using default title block '{fallback.FamilyName} - {fallback.Name}'.");
        return EnsureTitleBlockActive(doc, fallback);
    }

    private static FamilySymbol FindTitleBlockByName(
        Document doc,
        string familyName,
        string typeName)
    {
        foreach (var symbol in new FilteredElementCollector(doc)
                     .OfCategory(BuiltInCategory.OST_TitleBlocks)
                     .OfClass(typeof(FamilySymbol))
                     .Cast<FamilySymbol>())
        {
            var familyMatches = string.IsNullOrWhiteSpace(familyName) ||
                                symbol.FamilyName.Equals(familyName.Trim(), StringComparison.OrdinalIgnoreCase);
            var typeMatches = string.IsNullOrWhiteSpace(typeName) ||
                              symbol.Name.Equals(typeName.Trim(), StringComparison.OrdinalIgnoreCase);

            if (familyMatches && typeMatches)
                return symbol;
        }

        return null;
    }

    private static FamilySymbol EnsureTitleBlockActive(Document doc, FamilySymbol symbol)
    {
        if (!symbol.IsActive)
        {
            symbol.Activate();
            doc.Regenerate();
        }

        return symbol;
    }

    private static void ApplyRevisions(
        Document doc,
        ViewSheet sheet,
        SheetCreationInfo info,
        List<string> warnings)
    {
        if (info.RevisionIds == null || info.RevisionIds.Count == 0)
            return;

        warnings.Add(
            "revisionIds are accepted but automatic revision assignment on new sheets is not supported yet.");
    }

    private static string GetUniqueSheetNumber(Document doc, string requestedNumber)
    {
        var baseNumber = requestedNumber;
        var suffix = 1;
        var number = baseNumber;

        while (SheetNumberExists(doc, number))
            number = $"{baseNumber}-{suffix++}";

        return number;
    }

    private static bool SheetNumberExists(Document doc, string sheetNumber)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .Any(sheet => sheet.SheetNumber.Equals(sheetNumber, StringComparison.OrdinalIgnoreCase));
    }

    private static long GetElementIdValue(ElementId elementId)
    {
#if REVIT2024_OR_GREATER
        return elementId.Value;
#else
        return elementId.IntegerValue;
#endif
    }

    public string GetName() => "Create Sheet";
}
