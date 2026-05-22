namespace RevitMCPKgCommandSet.Models
{
    public class KgSessionInfoResult
    {
        public string ProjectId { get; set; }
        public string DocTitle { get; set; }
        public int Turn { get; set; }
        public int NodeCount { get; set; }
        public int EdgeCount { get; set; }
        public int PendingDeltaCount { get; set; }
        public int EsJournalLength { get; set; }
        public string LastActionSummary { get; set; }
    }
}
