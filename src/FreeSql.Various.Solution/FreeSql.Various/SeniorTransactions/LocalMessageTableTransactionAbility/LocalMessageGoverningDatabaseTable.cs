using FreeSql.DataAnnotations;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    [Table(Name = "various_local_message_governing")]
    [Index("governing_index", "governing")]
    internal class LocalMessageGoverningDatabaseTable : LocalMessageDatabaseTable
    {
        /// <summary>
        /// 所属调度者
        /// </summary>
        [Column(Name = "governing", StringLength = 100)]
        public string Governing { get; set; }

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