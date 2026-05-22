using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public interface IDeltaSink
    {
        void Emit(DeltaEntry entry);
    }

    public sealed class MemoryDeltaSink : IDeltaSink
    {
        private readonly List<DeltaEntry> _entries = new List<DeltaEntry>();

        public IReadOnlyList<DeltaEntry> Entries => _entries;
        public int Count => _entries.Count;

        public void Emit(DeltaEntry entry) => _entries.Add(entry);

        public void Clear() => _entries.Clear();
    }
}
