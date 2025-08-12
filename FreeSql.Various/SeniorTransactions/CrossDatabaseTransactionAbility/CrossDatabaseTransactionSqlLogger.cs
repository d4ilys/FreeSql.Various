using System.Text.Json;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    internal class CrossDatabaseTransactionSqlLogger
    {
        private static readonly AsyncLocal<List<string>> Logs =
            new AsyncLocal<List<string>>();

        internal static void Set(string log)
        {
            Logs.Value ??= [];
            Logs.Value.Add(log);
        }

        internal static void Clear()
        {
            Logs.Value?.Clear();
        }


        public static string Get()
        {
            return JsonSerializer.Serialize(Logs.Value);
        }
    }
}