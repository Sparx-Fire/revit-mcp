using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class ActionLogEntry
    {
        public int Turn { get; }
        public string Op { get; }
        public string TargetId { get; }
        public Dictionary<string, object> Payload { get; }

        public ActionLogEntry(int turn, string op, string targetId, Dictionary<string, object> payload)
        {
            Turn = turn;
            Op = op;
            TargetId = targetId;
            Payload = payload ?? new Dictionary<string, object>();
        }

        public ActionLogEntry Clone() =>
            new ActionLogEntry(Turn, Op, TargetId, new Dictionary<string, object>(Payload));
    }
}
