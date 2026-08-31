using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateGroupEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<GroupCreationInfo> GroupData { get; private set; }

        public AIResult<List<GroupResult>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<GroupCreationInfo> data)
        {
            GroupData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var createdGroups = new List<GroupResult>();
                _warnings.Clear();

                foreach (var info in GroupData)
                {
                    if (info.ElementIds == null || info.ElementIds.Count == 0)
                    {
                        _warnings.Add("No element IDs provided for group creation");
                        continue;
                    }

                    using (Transaction tx = new Transaction(_doc, "Create Group"))
                    {
                        tx.Start();

                        try
                        {
                            ICollection<ElementId> elementIds = info.ElementIds.Select(id => new ElementId(id)).ToList();

                            Group group = _doc.Create.NewGroup(elementIds);

                            if (group != null)
                            {
                                // Set group name if provided
                                if (!string.IsNullOrEmpty(info.Name))
                                {
                                    group.Name = info.Name;
                                }

                                createdGroups.Add(new GroupResult
                                {
                                    GroupId = group.Id.GetIntValue(),
                                    GroupTypeId = group.GroupType?.Id?.GetIntValue() ?? 0,
                                    Name = group.Name
                                });
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create group: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {createdGroups.Count} group(s)";
                if (_warnings.Count > 0)
                {
                    message += "\nWarnings:\n  " + string.Join("\n  ", _warnings);
                }

                Result = new AIResult<List<GroupResult>>
                {
                    Success = true,
                    Message = message,
                    Response = createdGroups
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<GroupResult>>
                {
                    Success = false,
                    Message = $"Error creating groups: {ex.Message}",
                };
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

        public string GetName()
        {
            return "Create Group";
        }
    }
}
