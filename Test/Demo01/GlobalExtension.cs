using FreeSql;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace Demo01
{
    public static class GlobalExtension
    {
        public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTable(
            this IUnitOfWork freeSqlUnitOfWork, string taskKey, string content)
        {
            var various = FreeSqlVariousInstance.Various;
            var localMessageTableTransactionUnitOfWorker =
                various.Transactions.LocalMessageTableTransaction.CreateUnitOfWorker();
            var tranFreeSql = freeSqlUnitOfWork.Orm;
            localMessageTableTransactionUnitOfWorker.Reliable(tranFreeSql, taskKey, content);
            return localMessageTableTransactionUnitOfWorker;
        }
    }
}