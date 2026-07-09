using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Access;

public class ElementPaginationTests : RevitApiTest
{
    private static Document _doc;
    private const int WallCount = 12;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Pagination Test");
        tx.Start();

        var level = Level.Create(_doc, 0.0);
        level.Name = "Pagination Test Level";

        for (int i = 0; i < WallCount; i++)
        {
            double x = i * 12.0;
            Wall.Create(_doc, Line.CreateBound(new XYZ(x, 0, 0), new XYZ(x + 10, 0, 0)), level.Id, false);
        }

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task FilterSetting_DefaultLimit_Is500()
    {
        var settings = new FilterSetting();
        await Assert.That(settings.GetEffectiveLimit()).IsEqualTo(500);
        await Assert.That(settings.GetEffectiveOffset()).IsEqualTo(0);
    }

    [Test]
    public async Task FilterSetting_MaxElementsFallback_WhenLimitNotSet()
    {
        var settings = new FilterSetting { Limit = 0, MaxElements = 25 };
        await Assert.That(settings.GetEffectiveLimit()).IsEqualTo(25);
    }

    [Test]
    public async Task FilterSetting_LimitTakesPrecedenceOverMaxElements()
    {
        var settings = new FilterSetting { Limit = 100, MaxElements = 25 };
        await Assert.That(settings.GetEffectiveLimit()).IsEqualTo(100);
    }

    [Test]
    public async Task AIElementFilter_GetFilteredElementIds_ReturnsAllMatchingWalls()
    {
        var settings = new FilterSetting
        {
            FilterCategory = "OST_Walls",
            IncludeInstances = true,
            IncludeTypes = false,
        };

        var ids = AIElementFilterEventHandler.GetFilteredElementIds(_doc, settings);
        await Assert.That(ids.Count).IsGreaterThanOrEqualTo(WallCount);
    }

    [Test]
    public async Task AIElementFilter_Pagination_LimitAndOffset()
    {
        var settings = new FilterSetting
        {
            FilterCategory = "OST_Walls",
            IncludeInstances = true,
            IncludeTypes = false,
            Limit = 5,
            Offset = 0,
        };

        var allIds = AIElementFilterEventHandler.GetFilteredElementIds(_doc, settings);
        int totalCount = allIds.Count;
        int limit = settings.GetEffectiveLimit();
        int offset = settings.GetEffectiveOffset();

        var page1 = allIds.Skip(offset).Take(limit).ToList();
        await Assert.That(page1.Count).IsEqualTo(5);
        await Assert.That(offset + page1.Count < totalCount).IsTrue();

        var page2 = allIds.Skip(5).Take(5).ToList();
        await Assert.That(page2.Count).IsEqualTo(5);
        await Assert.That(page1[0].GetIntValue()).IsNotEqualTo(page2[0].GetIntValue());
    }

    [Test]
    public async Task AIElementFilter_Pagination_LastPage_HasMoreFalse()
    {
        var settings = new FilterSetting
        {
            FilterCategory = "OST_Walls",
            IncludeInstances = true,
            IncludeTypes = false,
        };

        var allIds = AIElementFilterEventHandler.GetFilteredElementIds(_doc, settings);
        int totalCount = allIds.Count;
        int limit = 5;
        int offset = totalCount - (totalCount % limit == 0 ? limit : totalCount % limit);
        if (offset == totalCount)
            offset = Math.Max(0, totalCount - limit);

        var lastPage = allIds.Skip(offset).Take(limit).ToList();
        bool hasMore = offset + lastPage.Count < totalCount;

        await Assert.That(hasMore).IsFalse();
        await Assert.That(lastPage.Count).IsGreaterThan(0);
    }
}
