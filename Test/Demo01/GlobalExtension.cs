using System.Data.Common;
using FreeSql;
using FreeSql.Various;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace Demo01
{
    public static class GlobalExtension
    {
        public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTableEx(
            this IUnitOfWork freeSqlUnitOfWork, string taskKey, string content, bool activeDo)
        {
            var various = FreeSqlVariousInstance.Various;
            return freeSqlUnitOfWork.InjectLocalMessageTable(various, taskKey, content, activeDo);
        }
    }
}