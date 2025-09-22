using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.Various.Models;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    public class CrossDatabaseTransaction<TDbKey>
    {
        /// <summary>
        /// 创建跨数据库事务
        /// </summary>
        /// <param name="describe">事务描述</param>
        /// <param name="refers">事务涉及的数据库</param>
        /// <returns></returns>
        public CrossDatabaseTransactionAchieve<TDbKey> Create(string describe,
            params IEnumerable<FreeSqlElaborate<TDbKey>> refers)
        {
            return new CrossDatabaseTransactionAchieve<TDbKey>(refers, describe);
        }
    }
}