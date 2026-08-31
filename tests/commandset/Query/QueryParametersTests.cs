using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Query;

public class QueryParametersTests : RevitApiTest
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
    public async Task QueryParameters_GetBuiltInParams_ParametersFound()
    {
        var param = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
        await Assert.That(param).IsNotNull();
    }

    [Test]
    public async Task QueryParameters_EnumerateAll_ParametersEnumerated()
    {
        var paramSet = _wall.Parameters;
        int count = 0;
        foreach (var p in paramSet) count++;
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task QueryParameters_GetParameterValue_ValueRead()
    {
        var param = _wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        await Assert.That(param).IsNotNull();
        var value = param.AsString();
        await Assert.That(value).IsNotNull();
    }
}
