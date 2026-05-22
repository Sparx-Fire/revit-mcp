using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// IFailuresPreprocessor that silently drops every FailureSeverity.Warning
    /// raised during a Transaction. Errors (FailureSeverity.Error and above)
    /// are left alone — they still halt the txn, which is the right default
    /// for geometric integrity issues that the caller really should know
    /// about (e.g. circular references).
    ///
    /// Why this exists — Stage-3 80_m-long ($3.35 burned 2026-05-20) blocked
    /// on a "highlighted walls overlap" warning popup that no headless MCP
    /// session can dismiss. Without this, modify_* / create_* commands hang
    /// indefinitely on any geometric warning. With it, the user-facing
    /// modal disappears and the txn proceeds.
    /// </summary>
    public sealed class SwallowWarningsPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            a.DeleteAllWarnings();
            return FailureProcessingResult.Continue;
        }
    }

    /// <summary>
    /// Extension helpers to make the wire-once-and-Start pattern terse.
    /// </summary>
    public static class TransactionFailureHandling
    {
        /// <summary>
        /// Configure the Transaction to swallow Warnings and call Start() in
        /// one go. Equivalent to:
        ///   var opts = tx.GetFailureHandlingOptions();
        ///   opts.SetFailuresPreprocessor(new SwallowWarningsPreprocessor());
        ///   opts.SetClearAfterRollback(true);
        ///   tx.SetFailureHandlingOptions(opts);
        ///   tx.Start();
        /// </summary>
        public static TransactionStatus StartWithSwallowedWarnings(this Transaction tx)
        {
            var opts = tx.GetFailureHandlingOptions();
            opts.SetFailuresPreprocessor(new SwallowWarningsPreprocessor());
            opts.SetClearAfterRollback(true);
            tx.SetFailureHandlingOptions(opts);
            return tx.Start();
        }

        /// <summary>
        /// Same as above but with an explicit transaction name (Transaction
        /// API has a Start overload that takes a name to set/override).
        /// </summary>
        public static TransactionStatus StartWithSwallowedWarnings(this Transaction tx, string name)
        {
            var opts = tx.GetFailureHandlingOptions();
            opts.SetFailuresPreprocessor(new SwallowWarningsPreprocessor());
            opts.SetClearAfterRollback(true);
            tx.SetFailureHandlingOptions(opts);
            return tx.Start(name);
        }
    }
}
