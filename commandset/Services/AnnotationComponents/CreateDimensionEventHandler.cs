using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.AnnotationComponents;

/// <summary>
///     Handles creation of dimension elements in Revit.
/// </summary>
public class CreateDimensionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private UIApplication _uiApp;
    private UIDocument UiDoc => _uiApp.ActiveUIDocument;
    private Document Doc => UiDoc.Document;
    private readonly ManualResetEvent _resetEvent = new(false);

    public List<DimensionCreationInfo> DimensionsToCreate { get; private set; }
    public AIResult<List<int>> Result { get; private set; }

    public void SetParameters(List<DimensionCreationInfo> dimensions)
    {
        DimensionsToCreate = dimensions;
        _resetEvent.Reset();
    }

    public void Execute(UIApplication app)
    {
        _uiApp = app;
        try
        {
            var createdDimensionIds = new List<int>();

            foreach (var dimInfo in DimensionsToCreate)
            {
                View view = null;
                if (dimInfo.ViewId > 0)
                    view = Doc.GetElement(new ElementId(dimInfo.ViewId)) as View;

                view ??= Doc.ActiveView;

                using var transaction = new Transaction(Doc, "Create Dimension");
                transaction.Start();

                try
                {
                    var startPoint = DimensionAnnotationHelper.ConvertMmToFeet(dimInfo.StartPoint);
                    var endPoint = DimensionAnnotationHelper.ConvertMmToFeet(dimInfo.EndPoint);
                    var line = DimensionAnnotationHelper.BuildDimensionLine(
                        startPoint,
                        endPoint,
                        dimInfo.LinePoint,
                        dimInfo.OffsetMm);

                    Dimension dimension = null;

                    if (dimInfo.ElementIds != null && dimInfo.ElementIds.Count > 0)
                    {
                        var dimensionDirection = (endPoint - startPoint).Normalize();
                        var references = new ReferenceArray();
                        foreach (var elementId in dimInfo.ElementIds)
                        {
                            var element = Doc.GetElement(new ElementId(elementId));
                            if (element == null)
                                continue;

                            foreach (var reference in DimensionAnnotationHelper.GetReferences(
                                         element,
                                         view,
                                         dimensionDirection))
                            {
                                references.Append(reference);
                            }
                        }

                        if (references.Size >= 2)
                            dimension = Doc.Create.NewDimension(view, line, references);
                    }
                    else
                    {
                        var dimDirection = (endPoint - startPoint).Normalize();
                        var refArray = new ReferenceArray();
                        var startRef = DimensionAnnotationHelper.FindReferenceAtPoint(
                            Doc,
                            view,
                            startPoint,
                            dimDirection,
                            dimInfo.PickToleranceMm > 0
                                ? dimInfo.PickToleranceMm
                                : DimensionAnnotationHelper.DefaultPickToleranceMm);
                        var endRef = DimensionAnnotationHelper.FindReferenceAtPoint(
                            Doc,
                            view,
                            endPoint,
                            dimDirection,
                            dimInfo.PickToleranceMm > 0
                                ? dimInfo.PickToleranceMm
                                : DimensionAnnotationHelper.DefaultPickToleranceMm);

                        if (startRef != null && endRef != null)
                        {
                            refArray.Append(startRef);
                            refArray.Append(endRef);
                            dimension = Doc.Create.NewDimension(view, line, refArray);
                        }
                    }

                    if (dimension != null)
                    {
                        DimensionAnnotationHelper.ApplyDimensionType(
                            dimension,
                            Doc,
                            dimInfo.DimensionType,
                            dimInfo.DimensionStyleId);
                        ApplyDimensionParameters(dimension, dimInfo);
                        createdDimensionIds.Add(dimension.Id.GetIntValue());
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.RollBack();
                    throw;
                }
            }

            Result = new AIResult<List<int>>
            {
                Success = true,
                Message =
                    $"Successfully created {createdDimensionIds.Count} dimensions. ElementIds saved in Response.",
                Response = createdDimensionIds
            };
        }
        catch (Exception ex)
        {
            Result = new AIResult<List<int>>
            {
                Success = false,
                Message = $"Error creating dimensions: {ex.Message}",
                Response = new List<int>()
            };
            TaskDialog.Show("Error", $"Error creating dimensions: {ex.Message}");
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName() => "Create Dimension";

    private static void ApplyDimensionParameters(Dimension dimension, DimensionCreationInfo dimensionInfo)
    {
        if (dimensionInfo.Options == null)
            return;

        foreach (var option in dimensionInfo.Options)
        {
            var param = dimension.LookupParameter(option.Key);
            if (param == null)
                continue;

            if (option.Value is double doubleValue && param.StorageType == StorageType.Double)
                param.Set(doubleValue * DimensionAnnotationHelper.MillimetersToFeet);
            else if (option.Value is int intValue && param.StorageType == StorageType.Integer)
                param.Set(intValue);
            else if (option.Value is string stringValue && param.StorageType == StorageType.String)
                param.Set(stringValue);
        }
    }
}
