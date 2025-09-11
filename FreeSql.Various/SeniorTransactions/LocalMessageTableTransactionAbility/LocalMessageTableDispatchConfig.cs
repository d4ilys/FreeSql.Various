using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    public class LocalMessageTableDispatchConfig
    {
        /// <summary>
        /// 本地消息主表调度
        /// </summary>
        public LocalMessageTableDispatchSchedule MainSchedule { get; set; } = new();

        /// <summary>
        /// 本地消息分组表调度
        /// </summary>
        public Dictionary<string, LocalMessageTableGroupDispatchSchedule> GroupSchedules { get; set; } =
            new();

        /// <summary>
        /// 错误消息通知
        /// </summary>
        public Func<string, Task>? ErrorMessageNotice { get; set; }

        /// <summary>
        /// 幂等性重入，确保同一任务同一时间只能在同一个节点执行
        /// </summary>
        public Func<Func<Task>, Task>? IdempotentReentrant { get; set; }
    }

    public class LocalMessageTableDispatchSchedule
    {
        /// <summary>
        /// 调度间隔，默认2分钟
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// 调度器启动后多久开始执行，默认1秒
        /// </summary>
        public TimeSpan DueTime { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 最大重试次数，默认20次
        /// </summary>
        public int MaxRetries { get; set; } = 20;
    }

    public class LocalMessageTableGroupDispatchSchedule
    {
        /// <summary>
        /// 组中的任务是否强制保证顺序「如果其中一个任务失败回阻塞其后的所有任务」
        /// </summary>
        public bool GroupEnsureOrderliness { get; set; } = false;

        public LocalMessageTableDispatchSchedule Schedule { get; set; } = new LocalMessageTableDispatchSchedule();
    }
}