using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Annotation;

public class CreateRevisionCloudTests : RevitApiTest
{
    private static Document _doc;
    private static View _view;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _view = _doc.ActiveView;
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateRevisionCloud_CurveLoop_CloudCreated()
    {
        using var tx = new Transaction(_doc, "Create Revision Cloud");
        tx.Start();
        var loop = new CurveLoop();
        loop.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(5, 0, 0)));
        loop.Append(Line.CreateBound(new XYZ(5, 0, 0), new XYZ(5, 5, 0)));
        loop.Append(Line.CreateBound(new XYZ(5, 5, 0), new XYZ(0, 5, 0)));
        loop.Append(Line.CreateBound(new XYZ(0, 5, 0), new XYZ(0, 0, 0)));
        var revision = Revision.Create(_doc);
        var curves = new List<Curve>();
        foreach (var c in loop) curves.Add(c);
        var cloud = RevisionCloud.Create(_doc, _view, revision.Id, curves);
        tx.Commit();
        await Assert.That(cloud).IsNotNull();
    }
}
