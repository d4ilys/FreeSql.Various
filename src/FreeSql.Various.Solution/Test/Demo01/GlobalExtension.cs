using FreeSql;
using FreeSql.Various;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace Demo01
{
    public static class GlobalExtension
    {
        /// <summary>
        /// 添加本地消息表事务
        /// </summary>
        /// <param name="freeSqlUnitOfWork"></param>
        /// <param name="taskKey"></param>
        /// <param name="content"></param>
        /// <param name="governing"></param>
        /// <param name="group"></param>
        /// <param name="activeDo"></param>
        /// <returns></returns>
        public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTableEx(
            this IUnitOfWork freeSqlUnitOfWork, string taskKey, string content, string governing = "",
            string group = "", bool activeDo = true)
        {
            var various = FreeSqlVariousInstance.Various;
            return freeSqlUnitOfWork.InjectLocalMessageTable(various, taskKey, content, activeDo, governing, group);
        }
    }
}