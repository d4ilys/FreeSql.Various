using System.Text.Json;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    internal class CrossDatabaseTransactionSqlLogger
    {
        static readonly AsyncLocal<CrossDatabaseTransactionLoggerContext?> Context = new();

        internal static void StartLogger()
        {
            Context.Value ??= new CrossDatabaseTransactionLoggerContext();
            Context.Value.IsLogger = true;
        }

        internal static bool IsLogger()
        {
            return Context.Value!.IsLogger;
        }

        internal static void SetLogger(string log)
        {
            Context.Value!.Logs.Add(log);
        }

        internal static void Clear()
        {
            Context.Value = null;
        }

        internal static string GetLogger()
        {
            return Context.Value != null ? string.Join(Environment.NewLine, Context.Value.Logs) : string.Empty;
        }
    }

    internal class CrossDatabaseTransactionLoggerContext
    {
        public bool IsLogger { get; set; } = false;

        public List<string> Logs { get; set; } = new();
    }
}