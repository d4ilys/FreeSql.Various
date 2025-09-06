using System.Text.Json;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    internal class CrossDatabaseTransactionSqlLogger
    {
        private static readonly AsyncLocal<List<string>> Logs =
            new AsyncLocal<List<string>>();

        private static readonly AsyncLocal<bool> IsStartLogger = new();

        internal static void StartLogger()
        {
            IsStartLogger.Value = true;
        }

        internal static void StopLogger()
        {
            IsStartLogger.Value = false;
        }

        internal static bool IsLogger()
        {
            return IsStartLogger.Value;
        }

        internal static void SetLogger(string log)
        {
            Logs.Value ??= [];
            Logs.Value.Add(log);
        }

        internal static void Clear()
        {
            Logs.Value?.Clear();
            StopLogger();
        }


        public static string GetLogger()
        {
            return Logs.Value != null ? string.Join(",", Logs.Value) : string.Empty;
        }
    }
}