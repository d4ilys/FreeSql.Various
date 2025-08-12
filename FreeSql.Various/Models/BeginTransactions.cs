using System.Data.Common;

namespace FreeSql.Various.Models
{
    public class BeginTransactions(string databaseName, DbTransaction dbTransaction, bool isMaster)
    {
        public string DatabaseName { get; set; } = databaseName;

        public DbTransaction DbTransaction { get; set; } = dbTransaction;

        public bool IsMaster { get; set; } = isMaster;
    }
}