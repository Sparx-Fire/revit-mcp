using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateMEPCurveTests : RevitApiTest
{
    private static Document _doc;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateMEPCurve_MechanicalSystemTypeExists()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(Autodesk.Revit.DB.Mechanical.MechanicalSystemType))
            .Cast<Autodesk.Revit.DB.Mechanical.MechanicalSystemType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CreateMEPCurve_PipingSystemTypeExists()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType))
            .Cast<Autodesk.Revit.DB.Plumbing.PipingSystemType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }
}
