using FreeSql.DataAnnotations;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    [Table(Name = "various_local_message_group_table")]
    [Index("group_index", "group")]
    internal class LocalMessageGroupDatabaseTable : LocalMessageDatabaseTable
    {
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
    }
}