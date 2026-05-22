using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace RevitMCPKgCommandSet.Services
{
    // Append-only journal persisted in Revit ExtensibleStorage.
    //
    // History — why two schemas:
    //   v1 schema (Legacy*) wrote the ENTIRE journal back per Flush
    //   (read-modify-write whole-blob). O(journal_size) per write made the
    //   30-modify Stage-3 80_m-long scenario time out around turn 60. The
    //   chunk schema lets Flush create a NEW DataStorage per batch
    //   (O(N_pending) per write), at the cost of an O(N_chunks) Read.
    //
    // Migration is read-only and lossless: existing legacy entities stay
    // in place; subsequent Append calls write new chunks alongside. Read
    // returns legacy.jsonl + chunk1.jsonl + chunk2.jsonl ... in order.
    // No destructive migration is performed — a future compaction op can
    // merge chunks back into the legacy entity if needed.
    public static class KgV2ExtensibleStorage
    {
        // Legacy whole-blob schema (kept for reads; no longer the write path).
        private static readonly Guid LegacySchemaGuid =
            new Guid("A3F7D2E8-9B6C-4D5A-8E1F-3C4B5A6D7E8F");
        private const string LegacySchemaName = "RevitMcpKnowledgeGraphV2";

        // Chunked append-only schema (current write path).
        private static readonly Guid ChunkSchemaGuid =
            new Guid("B4E8C3F9-AC7D-5E6B-9F2A-4D5C6E7F8A9B");
        private const string ChunkSchemaName = "RevitMcpKnowledgeGraphV2Chunk";

        private const string VendorId = "MCPSERVERSFORREVIT";

        public const string WriteTransactionName = "KG v2 delta write";

        private const string FieldJsonl = "jsonl";
        private const string FieldChunkSeq = "chunk_seq";
        private const string FieldSchemaVersion = "schema_version";

        public const int CurrentSchemaVersion = 1;

        // ---- schema lookup / creation ----

        public static Schema GetOrCreateLegacySchema()
        {
            Schema schema = Schema.Lookup(LegacySchemaGuid);
            if (schema != null) return schema;

            var builder = new SchemaBuilder(LegacySchemaGuid);
            builder.SetSchemaName(LegacySchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetVendorId(VendorId);

            builder.AddSimpleField(FieldJsonl, typeof(string));
            builder.AddSimpleField(FieldSchemaVersion, typeof(int));

            return builder.Finish();
        }

        public static Schema GetOrCreateChunkSchema()
        {
            Schema schema = Schema.Lookup(ChunkSchemaGuid);
            if (schema != null) return schema;

            var builder = new SchemaBuilder(ChunkSchemaGuid);
            builder.SetSchemaName(ChunkSchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetVendorId(VendorId);

            builder.AddSimpleField(FieldJsonl, typeof(string));
            builder.AddSimpleField(FieldChunkSeq, typeof(int));
            builder.AddSimpleField(FieldSchemaVersion, typeof(int));

            return builder.Finish();
        }

        // Back-compat alias: existing code may call GetOrCreateSchema().
        // The legacy schema is the one tied to that original API.
        public static Schema GetOrCreateSchema() => GetOrCreateLegacySchema();

        // ---- DataStorage helpers ----

        // Find the (single) legacy DataStorage if it exists.
        public static DataStorage FindDataStorage(Document doc, Schema schema)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .WherePasses(new ExtensibleStorageFilter(schema.GUID));

            foreach (DataStorage ds in collector.Cast<DataStorage>())
            {
                Entity e = ds.GetEntity(schema);
                if (e != null && e.IsValid()) return ds;
            }
            return null;
        }

        // Enumerate all chunk DataStorages, ordered by chunk_seq.
        private static List<(int Seq, DataStorage Ds, Entity Entity)>
            FindChunkStorages(Document doc, Schema schema)
        {
            var list = new List<(int, DataStorage, Entity)>();
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .WherePasses(new ExtensibleStorageFilter(schema.GUID));
            foreach (DataStorage ds in collector.Cast<DataStorage>())
            {
                Entity e = ds.GetEntity(schema);
                if (e == null || !e.IsValid()) continue;
                int seq;
                try { seq = e.Get<int>(FieldChunkSeq); }
                catch { seq = 0; }
                list.Add((seq, ds, e));
            }
            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return list;
        }

        // ---- public API ----

        // Read the full journal: legacy whole-blob entity (if present)
        // followed by all chunks ordered by chunk_seq. Returns null when
        // the document has no v2-kg journal at all.
        public static string Read(Document doc)
        {
            if (doc == null) return null;

            var sb = new StringBuilder();
            bool anyContent = false;

            // Legacy single-entity content (pre-chunked).
            var legacySchema = GetOrCreateLegacySchema();
            var legacyDs = FindDataStorage(doc, legacySchema);
            if (legacyDs != null)
            {
                var entity = legacyDs.GetEntity(legacySchema);
                if (entity != null && entity.IsValid())
                {
                    var s = entity.Get<string>(FieldJsonl) ?? string.Empty;
                    if (s.Length > 0)
                    {
                        sb.Append(s);
                        if (!s.EndsWith("\n")) sb.Append('\n');
                        anyContent = true;
                    }
                }
            }

            // Chunked entries (ordered by chunk_seq).
            var chunkSchema = GetOrCreateChunkSchema();
            foreach (var (seq, ds, entity) in FindChunkStorages(doc, chunkSchema))
            {
                var s = entity.Get<string>(FieldJsonl) ?? string.Empty;
                if (s.Length == 0) continue;
                sb.Append(s);
                if (!s.EndsWith("\n")) sb.Append('\n');
                anyContent = true;
            }

            return anyContent ? sb.ToString() : null;
        }

        public static int ReadSchemaVersion(Document doc)
        {
            if (doc == null) return 0;
            var legacySchema = GetOrCreateLegacySchema();
            var legacyDs = FindDataStorage(doc, legacySchema);
            if (legacyDs != null)
            {
                var e = legacyDs.GetEntity(legacySchema);
                if (e != null && e.IsValid()) return e.Get<int>(FieldSchemaVersion);
            }
            // Fall back to any chunk's reported version (they all use the
            // current schema; first chunk found is fine).
            var chunkSchema = GetOrCreateChunkSchema();
            foreach (var (seq, ds, entity) in FindChunkStorages(doc, chunkSchema))
            {
                try { return entity.Get<int>(FieldSchemaVersion); }
                catch { return CurrentSchemaVersion; }
            }
            return 0;
        }

        // Append a chunk to the journal. O(N_pending) per call —
        // creates a new DataStorage with the next chunk_seq. Does NOT
        // re-read or rewrite the existing journal.
        public static void Append(Document doc, string jsonlChunk)
        {
            if (doc == null || string.IsNullOrEmpty(jsonlChunk)) return;
            var chunkSchema = GetOrCreateChunkSchema();
            using var tx = new Transaction(doc, WriteTransactionName);
            tx.Start();
            int nextSeq = NextChunkSeq(doc, chunkSchema);
            var ds = DataStorage.Create(doc);
            var entity = new Entity(chunkSchema);
            entity.Set<string>(FieldJsonl, jsonlChunk);
            entity.Set<int>(FieldChunkSeq, nextSeq);
            entity.Set<int>(FieldSchemaVersion, CurrentSchemaVersion);
            ds.SetEntity(entity);
            tx.Commit();
        }

        private static int NextChunkSeq(Document doc, Schema chunkSchema)
        {
            int max = 0;
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .WherePasses(new ExtensibleStorageFilter(chunkSchema.GUID));
            foreach (DataStorage ds in collector.Cast<DataStorage>())
            {
                Entity e = ds.GetEntity(chunkSchema);
                if (e == null || !e.IsValid()) continue;
                int seq;
                try { seq = e.Get<int>(FieldChunkSeq); }
                catch { continue; }
                if (seq > max) max = seq;
            }
            return max + 1;
        }

        // Whole-blob legacy write — kept for migration / compaction utility
        // (e.g., merge all chunks back into the legacy entity). NOT used
        // by EsDeltaSink any more; that path uses Append now.
        public static void Write(Document doc, string jsonl)
        {
            if (doc == null) return;
            var schema = GetOrCreateLegacySchema();
            using var tx = new Transaction(doc, WriteTransactionName);
            tx.Start();
            var ds = FindDataStorage(doc, schema);
            if (ds == null) ds = DataStorage.Create(doc);
            var entity = new Entity(schema);
            entity.Set<string>(FieldJsonl, jsonl ?? string.Empty);
            entity.Set<int>(FieldSchemaVersion, CurrentSchemaVersion);
            ds.SetEntity(entity);
            tx.Commit();
        }
    }
}
