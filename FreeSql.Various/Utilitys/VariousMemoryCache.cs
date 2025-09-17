using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.Utilitys
{
    internal class VariousMemoryCache
    {
        ///// <summary>
        ///// 初始化CurdAfter事件
        ///// </summary>
        //internal static readonly ConcurrentDictionary<string, Lazy<bool>> InitializedAopOnCurdAfter = new();

        /// <summary>
        ///  同步多库事务本地消息表
        /// </summary>
        internal static readonly ConcurrentDictionary<string, Lazy<bool>>
            InitializedCrossDatabaseTransactionLocalMessage = new();

        /// <summary>
        ///  同步本地消息表
        /// </summary>
        internal static readonly ConcurrentDictionary<Guid, Lazy<bool>> LazySyncLocalMessageTable = new();


        internal static readonly ConcurrentDictionary<int, bool> IsSyncLocalMessageTable = new();

        /// <summary>
        /// 本地消息表任务描述
        /// </summary>
        internal static readonly ConcurrentDictionary<string, string> LocalMessageTableTaskDescribe = new();

        /// <summary>
        /// 本地消息表事务任务
        /// </summary>
        internal static ConcurrentDictionary<string, Func<string, Task<bool>>>
            LocalMessageTableTransactionTasks = new();

        /// <summary>
        /// 本地消息表事务任务调度数据库
        /// </summary>
        internal static readonly HashSet<string> LocalMessageTableTransactionSchedulerKeys = new();


        internal static List<Timer> SchedulerTimers = new();

        internal static LocalMessageTableDispatchConfig LocalMessageTableDispatchConfig = new();
    }
}