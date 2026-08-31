using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class LoadFamilyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string FilePath { get; private set; }
        public string FamilyName { get; private set; }

        public AIResult<bool> Result { get; private set; }

        public void SetParameters(string filePath, string familyName)
        {
            FilePath = filePath;
            FamilyName = familyName;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                if (string.IsNullOrEmpty(FilePath))
                {
                    Result = new AIResult<bool> { Success = false, Message = "File path is required" };
                    return;
                }

                using (Transaction trans = new Transaction(doc, "Load Family"))
                {
                    trans.Start();

#if REVIT2026_OR_GREATER
                    // R26: LoadFamily with IFamilyLoadOptions removed
                    bool loaded = doc.LoadFamily(FilePath);
#elif REVIT2025_OR_GREATER
                    FamilyLoadOptions loadOptions = new FamilyLoadOptions();
                    bool loaded = doc.LoadFamily(FilePath, loadOptions);
#else
                    bool loaded = doc.LoadFamily(FilePath);
#endif

                    if (loaded)
                    {
                        trans.Commit();

                        if (!string.IsNullOrEmpty(FamilyName))
                        {
                            Family family = new FilteredElementCollector(doc)
                                .OfClass(typeof(Family))
                                .Cast<Family>()
                                .FirstOrDefault(f => f.Name == FamilyName);

                            if (family == null)
                            {
                                Result = new AIResult<bool>
                                {
                                    Success = true,
                                    Message = $"Family loaded from '{FilePath}' but specified family name '{FamilyName}' not found in project",
                                    Response = true
                                };
                                return;
                            }
                        }

                        Result = new AIResult<bool>
                        {
                            Success = true,
                            Message = $"Family loaded successfully from '{FilePath}'",
                            Response = true
                        };
                    }
                    else
                    {
                        trans.RollBack();
                        Result = new AIResult<bool>
                        {
                            Success = false,
                            Message = $"Failed to load family from '{FilePath}'",
                            Response = false
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error loading family: {ex.Message}",
                    Response = false
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 30000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Load Family";
    }

    public class FamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}
