using FreeSql.DataAnnotations;
using FreeSql.Various.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql;

namespace Demo01.TestClass
{
    public class SharingDatabaseManager
    {
        /// <summary> 
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 租户Id
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// 分库模式
        /// </summary>
        public VariousSharingPatternEnum SharingPattern { get; set; }

        /// <summary>
        /// 配置Json
        /// </summary>
        [JsonMap]
        public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// <summary>
        /// 数据库枚举名称
        /// </summary>
        /// </summary>
        public string DbEnumName { get; set; }
    }

    public class AllDatabaseConnectionManager
    {
        /// <summary> 
        /// Id
        /// </summary>
        [Column(IsPrimary = true, IsIdentity = true)]
        public int Id { get; set; }

        public int DatabaseId { get; set; }
         
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// 租户数据库链接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataType DataType { get; set; }
    }
}