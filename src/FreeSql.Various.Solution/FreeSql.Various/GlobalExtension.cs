using FreeSql.Various.Dashboard;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace FreeSql.Various
{
    public static class GlobalExtension
    {
        public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTable<TDbKey>(
            this IUnitOfWork freeSqlUnitOfWork, FreeSqlVarious<TDbKey> various, string taskKey,
            string content, bool activeDo, string governing = "", string group = "")
            where TDbKey : notnull
        {
            var localMessageTableTransactionUnitOfWorker =
                various.SeniorTransactions.LocalMessageTableTransaction.CreateUnitOfWorker();

            //事务Fsql对象
            var tranFreeSql = freeSqlUnitOfWork.Orm;
            localMessageTableTransactionUnitOfWorker.Reliable(tranFreeSql, taskKey, content, governing, group);

            //如果主动触发执行 则携带到UnitOfWorker通过Aop执行
            if (!activeDo)
            {
                return localMessageTableTransactionUnitOfWorker;
            }

            const string stateKey = "FreeSqlUnitOfWorkStatesCarrier";

            //主动触发执行逻辑
            if (!freeSqlUnitOfWork.States.ContainsKey(stateKey))
            {
                freeSqlUnitOfWork.States[stateKey] =
                    new List<FreeSqlUnitOfWorkStatesCarrier>();
            }

            if (freeSqlUnitOfWork.States[stateKey] is List<FreeSqlUnitOfWorkStatesCarrier>
                list)
            {
                list.Add(
                    new FreeSqlUnitOfWorkStatesCarrier
                    {
                        Group = group,
                        Governing = governing,
                        UnitOfWorker = localMessageTableTransactionUnitOfWorker
                    });
            }

            return localMessageTableTransactionUnitOfWorker;
        }

        public static void Add(this IList<VariousDashboardCustomExecutor> list,
            Action<VariousDashboardCustomExecutor> action)
        {
            var exe = new VariousDashboardCustomExecutor();
            action(exe);
            list.Add(exe);
        }
    }
}