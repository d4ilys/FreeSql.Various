using System.Data.Common;
using FreeSql.Various.Models;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    public class CrossDatabaseTransactionFreeSql(IFreeSql pureOrm, DbTransaction transaction)
    {
        internal readonly IFreeSql PureOrm = pureOrm;

        private readonly DbTransaction _transaction = transaction ?? throw new Exception("指定的事务不存在.");

        public IDelete<T1> Delete<T1>() where T1 : class => PureOrm.Delete<T1>().WithTransaction(_transaction);

        public IDelete<T1> Delete<T1>(object dynamicWhere) where T1 : class => Delete<T1>().WhereDynamic(dynamicWhere);

        public IUpdate<T1> Update<T1>() where T1 : class => PureOrm.Update<T1>().WithTransaction(_transaction);

        public IUpdate<T1> Update<T1>(object dynamicWhere) where T1 : class => Update<T1>().WhereDynamic(dynamicWhere);

        public IInsert<T1> Insert<T1>() where T1 : class => PureOrm.Insert<T1>().WithTransaction(_transaction);

        public IInsert<T1> Insert<T1>(T1 source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsert<T1> Insert<T1>(T1[] source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsert<T1> Insert<T1>(List<T1> source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsert<T1> Insert<T1>(IEnumerable<T1> source) where T1 : class => Insert<T1>().AppendData(source);

        public IInsertOrUpdate<T1> InsertOrUpdate<T1>() where T1 : class =>
            PureOrm.InsertOrUpdate<T1>().WithTransaction(_transaction);
    }

    public class CrossDatabaseTransactionFreeSqlAggregate<TDbKey>
    {
        private readonly IList<CrossDatabaseTransactionFreeSql> _freeSqls = new List<CrossDatabaseTransactionFreeSql>();

        internal void Add(CrossDatabaseTransactionFreeSql freeSql)
        {
            _freeSqls.Add(freeSql);
        }

        public CrossDatabaseTransactionFreeSql Orm1 => _freeSqls[0];

        public CrossDatabaseTransactionFreeSql Orm2 => _freeSqls[1];

        public CrossDatabaseTransactionFreeSql Orm3 => _freeSqls[2];
        

        public CrossDatabaseTransactionFreeSql Orm4 => _freeSqls[3];
        

        public CrossDatabaseTransactionFreeSql Orm5 => _freeSqls[4];
        

        public CrossDatabaseTransactionFreeSql Orm6 => _freeSqls[5];
    }
}