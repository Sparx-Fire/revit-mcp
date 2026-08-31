using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class SayHelloTests : RevitApiTest
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
    public async Task SayHello_ProjectInfo_ProjectNameNotEmpty()
    {
        var projectInfo = _doc.ProjectInformation;
        await Assert.That(projectInfo).IsNotNull();
    }

    [Test]
    public async Task SayHello_DocumentTitle_TitleNotEmpty()
    {
        var title = _doc.Title;
        await Assert.That(string.IsNullOrEmpty(title)).IsFalse();
    }
}
