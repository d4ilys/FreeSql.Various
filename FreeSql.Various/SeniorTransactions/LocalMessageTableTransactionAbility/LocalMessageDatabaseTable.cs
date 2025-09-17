using FreeSql.DataAnnotations;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    [Table(Name = "various_local_message")]
    internal class LocalMessageDatabaseTable
    {
        [Column(Name = "id", IsPrimary = true, StringLength = 50)]
        public string Id { get; set; }

        /// <summary>
        /// 任务Id
        /// </summary>
        [Column(Name = "task_key", StringLength = 100)]
        public string TaskKey { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [Column(Name = "task_describe", StringLength = 300)]
        public string TaskDescribe { get; set; }

        /// <summary>
        /// 消息时间
        /// </summary>
        [Column(Name = "message_time", IsNullable = false)]
        public DateTime MessageTime { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        [Column(Name = "retries")]
        public int Retries { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        [Column(Name = "message_content", StringLength = -1)]
        public string MessageContent { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [Column(Name = "error_message", StringLength = -1)]
        public string ErrorMessage { get; set; }
    }

    [Table(Name = "various_local_message_log")]
    internal class LocalMessageDatabaseTableLogger
    {
        [Column(Name = "id", IsPrimary = true, StringLength = 50)]
        public string Id { get; set; }

        /// <summary>
        /// 任务Id
        /// </summary>
        [Column(Name = "task_key", StringLength = 100)]
        public string TaskKey { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        [Column(Name = "execution_count")]
        public int ExecutionCount { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        [Column(Name = "task_describe", StringLength = 300)]
        public string TaskDescribe { get; set; }

        /// <summary>
        /// 消息时间
        /// </summary>
        [Column(Name = "message_time", IsNullable = false)]
        public DateTime MessageTime { get; set; }


        /// <summary>
        /// 消息内容
        /// </summary>
        [Column(Name = "message_content", StringLength = -1)]
        public string MessageContent { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [Column(Name = "error_message", StringLength = -1)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 任务组
        /// </summary>
        [Column(Name = "group", StringLength = 100)]
        public string Group { get; set; }

        /// <summary>
        /// 分组后是否保证有序
        /// </summary>
        [Column(Name = "group_ensure_orderliness")]
        public bool GroupEnsureOrderliness { get; set; }

        /// <summary>
        /// 所属调度者
        /// </summary>
        [Column(Name = "governing", StringLength = 100)]
        public string Governing { get; set; }
    }
}