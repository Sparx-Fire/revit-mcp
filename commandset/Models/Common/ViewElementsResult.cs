namespace RevitMCPCommandSet.Models.Common
{
    public class ViewElementsResult
    {
        public long ViewId { get; set; }
        public string ViewName { get; set; }
        public int TotalElementsInView { get; set; }
        public int FilteredElementCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public List<ElementInfo> Elements { get; set; } = new List<ElementInfo>();
    }
}
