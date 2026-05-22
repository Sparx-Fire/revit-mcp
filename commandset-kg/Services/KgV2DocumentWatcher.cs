using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Core;

namespace RevitMCPKgCommandSet.Services
{
    public static class KgV2DocumentWatcher
    {
        private const int ProjectIdLen = 16;

        private static readonly object _lock = new object();
        private static bool _subscribed;
        private static Application _app;
        private static ExternalEvent _flushEvent;

        private static readonly Dictionary<string, ProjectKg> _projects =
            new Dictionary<string, ProjectKg>();
        private static readonly Dictionary<string, EsDeltaSink> _sinks =
            new Dictionary<string, EsDeltaSink>();
        private static string _currentDocKey = string.Empty;
        private static string _currentDocTitle = string.Empty;

        public static void EnsureSubscribed(UIApplication uiApp)
        {
            if (uiApp == null) return;
            var app = uiApp.Application;
            if (app == null) return;
            lock (_lock)
            {
                if (!_subscribed)
                {
                    try
                    {
                        app.DocumentChanged += OnDocumentChanged;
                        app.DocumentOpened += OnDocumentOpened;
                        app.DocumentSaving += OnDocumentSaving;
                        app.DocumentSavingAs += OnDocumentSavingAs;
                        _app = app;
                        _subscribed = true;

                        try
                        {
                            if (_flushEvent == null)
                                _flushEvent = ExternalEvent.Create(new KgV2FlushExternalEventHandler());
                        }
                        catch
                        {
                            // Fallback : if we can't create the ExternalEvent from
                            // this context, OnDocumentChanged will Flush() inline
                            // (best-effort, exceptions swallowed in the sink).
                        }

                        var first = app.Documents
                            ?.Cast<Document>()
                            .FirstOrDefault(d => d != null && !d.IsLinked);
                        if (first != null) BootstrapDocument(first);
                    }
                    catch
                    {
                        // Subscription best-effort, never break the caller.
                    }
                }

                // Lazy re-align to the active document on every call.
                // Covers File→New (no DocumentOpened event), inter-doc
                // ViewActivated, and any path where the active doc has drifted
                // from _currentDocKey (e.g. plugin "Switch" UI without rebind).
                try
                {
                    var active = uiApp.ActiveUIDocument?.Document;
                    if (active != null && !active.IsLinked)
                    {
                        var activeKey = ProjectIdFor(active);
                        if (activeKey != _currentDocKey || !_projects.ContainsKey(activeKey))
                        {
                            BootstrapDocument(active);
                        }
                    }
                }
                catch
                {
                    // Re-align best-effort, never break the caller.
                }
            }
        }

        public static void FlushCurrent()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_currentDocKey)) return;
                if (_sinks.TryGetValue(_currentDocKey, out var sink)) sink.Flush();
            }
        }

        public static ProjectKg GetCurrentProjectKg()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_currentDocKey)) return null;
                return _projects.TryGetValue(_currentDocKey, out var kg) ? kg : null;
            }
        }

        public static string CurrentDocTitle
        {
            get { lock (_lock) { return _currentDocTitle; } }
        }

        public static int CurrentPendingCount
        {
            get
            {
                lock (_lock)
                {
                    if (string.IsNullOrEmpty(_currentDocKey)) return 0;
                    return _sinks.TryGetValue(_currentDocKey, out var sink) ? sink.PendingCount : 0;
                }
            }
        }

        public static int ReadEsJournalLength(Document doc)
        {
            try
            {
                var s = KgV2ExtensibleStorage.Read(doc);
                return s?.Length ?? 0;
            }
            catch { return -1; }
        }

        // ---- bootstrap ----

        private static void BootstrapDocument(Document doc)
        {
            try
            {
                _currentDocKey = ProjectIdFor(doc);
                _currentDocTitle = doc.Title ?? string.Empty;
                if (_projects.ContainsKey(_currentDocKey)) return;

                var sink = new EsDeltaSink(doc);
                var existing = KgV2ExtensibleStorage.Read(doc);

                ProjectKg kg;
                if (!string.IsNullOrEmpty(existing))
                {
                    var entries = JsonlSerializer.DeserializeAll(existing);
                    var (replayed, _) = ProjectKgReplay.Replay(_currentDocKey, entries);
                    kg = replayed;
                    kg.AttachSink(sink);
                }
                else
                {
                    kg = new ProjectKg(_currentDocKey);
                    kg.AttachSink(sink);
                    var reader = new RevitElementReader(doc);
                    ScanAndProject(doc, kg, reader, typeof(Level));
                    ScanAndProject(doc, kg, reader, typeof(WallType));
                    ScanFamilySymbols(doc, kg, reader);
                    ScanAndProject(doc, kg, reader, typeof(Wall));
                    ScanFamilyInstances(doc, kg, reader);
                    ScanRooms(doc, kg, reader);
                    sink.Flush();
                }

                _projects[_currentDocKey] = kg;
                _sinks[_currentDocKey] = sink;
            }
            catch { }
        }

        private static void ScanAndProject(Document doc, ProjectKg kg, RevitElementReader reader, Type ofClass)
        {
            var ids = new FilteredElementCollector(doc)
                .OfClass(ofClass)
                .WhereElementIsNotElementType()
                .ToElementIds();
            var typeCollIds = new FilteredElementCollector(doc)
                .OfClass(ofClass)
                .WhereElementIsElementType()
                .ToElementIds();
            var all = ids.Concat(typeCollIds).Select(e => e.Value).ToList();
            Projection.ApplyAdded(kg, reader, all);
        }

        private static void ScanFamilySymbols(Document doc, ProjectKg kg, RevitElementReader reader)
        {
            var ids = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(fs => fs.Category != null &&
                             (fs.Category.Id.Value == (long)BuiltInCategory.OST_Windows ||
                              fs.Category.Id.Value == (long)BuiltInCategory.OST_Doors))
                .Select(fs => fs.Id.Value)
                .ToList();
            Projection.ApplyAdded(kg, reader, ids);
        }

        private static void ScanFamilyInstances(Document doc, ProjectKg kg, RevitElementReader reader)
        {
            var ids = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => fi.Category != null &&
                             (fi.Category.Id.Value == (long)BuiltInCategory.OST_Windows ||
                              fi.Category.Id.Value == (long)BuiltInCategory.OST_Doors))
                .Select(fi => fi.Id.Value)
                .ToList();
            Projection.ApplyAdded(kg, reader, ids);
        }

        // Rooms are scanned last so their bounded_by edges resolve against
        // walls already projected this bootstrap pass.
        private static void ScanRooms(Document doc, ProjectKg kg, RevitElementReader reader)
        {
            var ids = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Select(e => e.Id.Value)
                .ToList();
            Projection.ApplyAdded(kg, reader, ids);
        }

        // ---- events (never throw) ----

        private static void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    var txns = e.GetTransactionNames();
                    bool onlyOurWrites = txns != null && txns.Count > 0 &&
                        txns.All(n => n == KgV2ExtensibleStorage.WriteTransactionName);
                    if (onlyOurWrites) return;

                    var doc = e.GetDocument();
                    if (doc == null) return;
                    var key = ProjectIdFor(doc);
                    if (!_projects.TryGetValue(key, out var kg)) return;

                    var reader = new RevitElementReader(doc);
                    var added = e.GetAddedElementIds()?.Select(id => id.Value).ToList()
                                ?? new List<long>();
                    var modified = e.GetModifiedElementIds()?.Select(id => id.Value).ToList()
                                   ?? new List<long>();
                    var deleted = e.GetDeletedElementIds()?.Select(id => id.Value).ToList()
                                  ?? new List<long>();

                    if (added.Count > 0) Projection.ApplyAdded(kg, reader, added);
                    if (modified.Count > 0) Projection.ApplyModified(kg, reader, modified);
                    if (deleted.Count > 0) Projection.ApplyDeleted(kg, deleted);

                    kg.AdvanceTurn();

                    if (_sinks.TryGetValue(key, out var sink) && sink.HasPending)
                    {
                        if (_flushEvent != null) _flushEvent.Raise();
                        else sink.Flush();
                    }
                }
            }
            catch { }
        }

        private static void OnDocumentSaving(object sender, DocumentSavingEventArgs e)
        {
            try { FlushBeforeSave(e?.Document); }
            catch { }
        }

        private static void OnDocumentSavingAs(object sender, DocumentSavingAsEventArgs e)
        {
            try { FlushBeforeSave(e?.Document); }
            catch { }
        }

        private static void FlushBeforeSave(Document doc)
        {
            if (doc == null) return;
            lock (_lock)
            {
                var key = ProjectIdFor(doc);
                if (_sinks.TryGetValue(key, out var sink) && sink.HasPending)
                    sink.Flush();
            }
        }

        private static void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    if (e.Document != null) BootstrapDocument(e.Document);
                }
            }
            catch { }
        }

        // ---- project_id (L-8 : hash PathName, fallback title) ----

        private static string ProjectIdFor(Document doc)
        {
            if (doc == null) return "unknown";
            var seed = (doc.PathName ?? string.Empty).Trim();
            if (seed.Length == 0) seed = $"title:{doc.Title ?? "untitled"}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, ProjectIdLen);
        }
    }
}
