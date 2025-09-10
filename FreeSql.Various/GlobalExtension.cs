using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various
{
    public static class GlobalExtension
    {
        public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTable<TDbKey>(
            this IUnitOfWork freeSqlUnitOfWork, FreeSqlVarious<TDbKey> various, string taskKey,
            string content, bool activeDo)
            where TDbKey : notnull
        {
            var localMessageTableTransactionUnitOfWorker =
                various.Transactions.LocalMessageTableTransaction.CreateUnitOfWorker();

            //事务Fsql对象
            var tranFreeSql = freeSqlUnitOfWork.Orm;
            localMessageTableTransactionUnitOfWorker.Reliable(tranFreeSql, taskKey, content);

            //如果主动触发执行 则携带到UnitOfWorker通过Aop执行
            if (activeDo)
                freeSqlUnitOfWork.States["LocalMessageTableTransaction"] = localMessageTableTransactionUnitOfWorker;
        

            return localMessageTableTransactionUnitOfWorker;
        }
    }
}