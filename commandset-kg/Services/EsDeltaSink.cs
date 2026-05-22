using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;
using RevitMCPKgCommandSet.Core;

namespace RevitMCPKgCommandSet.Services
{
    public sealed class EsDeltaSink : IDeltaSink
    {
        private readonly Document _doc;
        private readonly List<DeltaEntry> _pending = new List<DeltaEntry>();
        private readonly object _lock = new object();

        public EsDeltaSink(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        public void Emit(DeltaEntry entry)
        {
            if (entry == null) return;
            lock (_lock) _pending.Add(entry);
        }

        public bool HasPending
        {
            get { lock (_lock) return _pending.Count > 0; }
        }

        public int PendingCount
        {
            get { lock (_lock) return _pending.Count; }
        }

        public void Flush()
        {
            List<DeltaEntry> toFlush;
            lock (_lock)
            {
                if (_pending.Count == 0) return;
                toFlush = new List<DeltaEntry>(_pending);
            }

            try
            {
                // Append-only chunked persist (O(N_pending) per call).
                // The serialised chunk holds ONLY the new entries —
                // KgV2ExtensibleStorage.Append creates a new DataStorage
                // with the next chunk_seq; Read combines all chunks +
                // any legacy whole-blob entity on demand.
                var sb = new StringBuilder(256 * toFlush.Count);
                foreach (var entry in toFlush)
                {
                    sb.Append(JsonlSerializer.SerializeOne(entry));
                    sb.Append('\n');
                }
                KgV2ExtensibleStorage.Append(_doc, sb.ToString());

                lock (_lock)
                {
                    if (_pending.Count == toFlush.Count) _pending.Clear();
                    else _pending.RemoveRange(0, toFlush.Count);
                }
            }
            catch
            {
                // Best-effort persist. Pending entries stay buffered for the
                // next Flush attempt. A logger here would be useful when the
                // RevitMCPSDK logging pattern is plumbed into the watcher.
            }
        }
    }
}
