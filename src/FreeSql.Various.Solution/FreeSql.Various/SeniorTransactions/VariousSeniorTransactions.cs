using FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace FreeSql.Various.SeniorTransactions
{
    public class VariousSeniorTransactions<TDbKey>(FreeSqlVarious<TDbKey> various) where TDbKey : notnull
    {
        /// <summary>
        /// 多库事务
        /// </summary>
        public CrossDatabaseTransaction<TDbKey> CrossDatabaseTransaction { get; private set; } = new();

        /// <summary>
        /// 本地消息表事务
        /// </summary>
        public LocalMessageTableTransaction<TDbKey> LocalMessageTableTransaction { get; private set; } = new(various);
    }
}