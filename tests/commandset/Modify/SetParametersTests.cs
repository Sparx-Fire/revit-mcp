using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class SetParametersTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static Wall _wall;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task SetParameters_Comments_StringParameterSet()
    {
        using var tx = new Transaction(_doc, "Set Comments");
        tx.Start();
        var param = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        if (param != null && !param.IsReadOnly)
            param.Set("Test Comment");
        tx.Commit();
        var readParam = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        await Assert.That(readParam?.AsString()).IsEqualTo("Test Comment");
    }

    [Test]
    public async Task SetParameters_Mark_MarkParameterSet()
    {
        using var tx = new Transaction(_doc, "Set Mark");
        tx.Start();
        var param = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        if (param != null && !param.IsReadOnly)
            param.Set("W-001");
        tx.Commit();
        var readParam = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        await Assert.That(readParam?.AsString()).IsEqualTo("W-001");
    }

    [Test]
    public async Task SetParameters_RollbackOnFailure_ParameterUnchanged()
    {
        var originalMark = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
        using (var tx = new Transaction(_doc, "Rollback Mark"))
        {
            tx.Start();
            var param = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (param != null && !param.IsReadOnly)
                param.Set("ROLLBACK");
            tx.RollBack();
        }
        var afterMark = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
        await Assert.That(afterMark).IsEqualTo(originalMark);
    }
}
