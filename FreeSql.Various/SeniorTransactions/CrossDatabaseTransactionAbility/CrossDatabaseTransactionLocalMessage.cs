using FreeSql.DataAnnotations;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    /// <summary>
    /// 记录事务执行的日志
    /// </summary>
    [Table(Name = "various_cross_database_transaction_local_message")]
    public class CrossDatabaseTransactionLocalMessage
    {
        [Column(IsIdentity = true, Name = "id")]
        public long Id { get; set; }

        /// <summary>
        /// 事务内容描述
        /// </summary>
        [Column(StringLength = 1000, Name = "describe")]
        public string? Describe { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column(Name = "create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 执行SQL
        /// </summary>
        [Column(StringLength = -1, Name = "exec_sql")]
        public string? ExecSql { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        [Column(StringLength = -1, Name = "result_msg")]
        public string? ResultMsg { get; set; }

        /// <summary>
        /// 是否成功 0成功、1失败
        /// </summary>
        [Column(Name = "successful")]
        public bool Successful { get; set; } = true;
    }
}