using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    internal class CrossDatabaseTransactionCache
    {
        //初始化CurdAfter事件
        public static readonly ConcurrentDictionary<string, Lazy<bool>> InitializedAopOnCurdAfter = new();

    }
}
