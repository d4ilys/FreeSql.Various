namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    internal class CrossDatabaseTransactionExecOutcome(string databaseName, bool success, string? errorMessage)
    {
        public string DatabaseName { get; set; } = databaseName;

        public bool Success { get; set; } = success;

        public string? ErrorMessage { get; set; } = errorMessage;
    }
}