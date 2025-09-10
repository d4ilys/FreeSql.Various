using System.Collections.Concurrent;
using FreeSql.Internal.ObjectPool;
using FreeSql.Various.Models;
using FreeSql.Various.Utilitys;
using System.Data.Common;
using System.IO.Pipelines;
using System.Text.Json;
using FreeSql.Aop;

namespace FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility
{
    public class CrossDatabaseTransactionAchieve<TDbKey>(
        IEnumerable<FreeSqlElaborate<TDbKey>> refers,
        string describe = "") : IDisposable
    {
        //连接池 
        private Dictionary<string, Object<DbConnection>> _connections = new();
      
        //用到的事务
        internal List<BeginTransactions> Transactions = new();

        //日志ID
        private long _logId = 0;

        //监听错误提交事件
        internal event Action<long>? OnCommitFail = null;

        /// <summary>
        /// 开启事务的FreeSql对象
        /// </summary>
        public CrossDatabaseTransactionFreeSqlAggregate<TDbKey> Orms { get; } =
            new();

        //每个sql执行都会触发
        private void AopOnCurdAfter(object? sender, CurdAfterEventArgs args)
        {
            if (args.CurdType != CurdType.Select && CrossDatabaseTransactionSqlLogger.IsLogger())
            {
                if (args.DbParms.Any())
                {
                    var pars = args.DbParms;
                    var sql =
                        $"[Sql]:{args.Sql}{Environment.NewLine}[DbParms]:{string.Join(" ", pars.Select(it => $"{Environment.NewLine}[Database]:{it.ParameterName} [Value]:{it.Value} [Type]:{it.DbType}  "))}";

                    CrossDatabaseTransactionSqlLogger.SetLogger(sql);
                }
                else
                {
                    CrossDatabaseTransactionSqlLogger.SetLogger(args.Sql);
                }
            }
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public void Begin()
        {
            var isMaster = true;

            //事务开始 才需要记录日志
            CrossDatabaseTransactionSqlLogger.StartLogger();

            foreach (var dbInstance in refers)
            {
                var db = dbInstance.FreeSql;

                //获取连接池对象
                var dbConnection = db.Ado.MasterPool.Get();

                //添加到字典用于归还
                _connections.TryAdd(dbInstance.Database, dbConnection);

                var lazyInit = VariousMemoryCache.InitializedAopOnCurdAfter.GetOrAdd(dbInstance.Database, s => new Lazy<bool>(() =>
                {
                    db.Aop.CurdAfter += AopOnCurdAfter;
                    return true;
                }));

                //对应的数据库Lazy添加Aop拦截
                _ = lazyInit.Value;

                //获取事务对象
                var transaction = dbConnection.Value.BeginTransaction();

                Transactions.Add(new BeginTransactions(dbInstance.Database, transaction, isMaster));

                Orms.Add(new CrossDatabaseTransactionFreeSql(db, transaction));

                if (isMaster) isMaster = false;
            }
        }

        //提交事务
        //这里的思路：第一个事务必须成功，因为第一个事务需要记录这个事务执行的信息，如果它Common失败，其他事务全部回滚
        public bool Commit()
        {
            var deputyTransactionOutcomes = new List<CrossDatabaseTransactionExecOutcome>();

            bool masterCommitSuccess = true;

            foreach (var item in Transactions)
            {
                if (item.IsMaster)
                {
                    masterCommitSuccess = MasterTransactionCommit(item.DatabaseName, item.DbTransaction);
                    if (masterCommitSuccess == false) break;
                }
                else
                {
                    (bool, Exception?) otherTransactionCommit =
                        DeputyTransactionOutcomesTransactionCommit(item.DatabaseName, item.DbTransaction);
                    deputyTransactionOutcomes.Add(new CrossDatabaseTransactionExecOutcome(item.DatabaseName,
                        otherTransactionCommit.Item1, otherTransactionCommit.Item2?.Message));
                }
            }

            //如果主事务失败，直接返回
            if (masterCommitSuccess == false) return false;

            var isSuccess = deputyTransactionOutcomes.All(t => t.Success);

            var masterFreeSql = refers.First().FreeSql;

            try
            {
                if (isSuccess)
                {
                    //删除日志
                    masterFreeSql.Delete<CrossDatabaseTransactionLocalMessage>().Where(m => m.Id == _logId)
                        .ExecuteAffrows();
                }
                else
                {
                    var deputyComeout = JsonSerializer.Serialize(deputyTransactionOutcomes);

                    //更新日志
                    masterFreeSql.Update<CrossDatabaseTransactionLocalMessage>()
                        .Set(t => t.Successful, false)
                        .Set(t => t.ResultMsg, deputyComeout)
                        .Where(t => t.Id == _logId)
                        .ExecuteAffrows();
                }
            }
            catch (Exception e)
            {
                VariousConsole.Error<CrossDatabaseTransactionAchieve<TDbKey>>($"【多库事务提交失败修改日志信息失败「{_logId}」】发生异常 {e}.");
            }
            finally
            {
                CrossDatabaseTransactionSqlLogger.Clear();
                if (isSuccess == false) //事件注册
                    OnCommitFail?.Invoke(_logId);
            }

            return isSuccess;
        }

        private bool MasterTransactionCommit(string dbName,
            DbTransaction transaction)
        {
            try
            {
                var firstIFreeSql = refers.First().FreeSql;

                var log = new CrossDatabaseTransactionLocalMessage
                {
                    Describe = describe,
                    CreateTime = DateTime.Now,
                    ExecSql = CrossDatabaseTransactionSqlLogger.GetLogger(),
                    ResultMsg = null,
                    Successful = true
                };

                _logId = firstIFreeSql.Insert(log).WithTransaction(transaction)
                    .ExecuteIdentity();

                //第一个Common在提交的时候由于数据库宕机或者是其他原因导致无法提交，全部Rollback
                transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                Rollback();
                VariousConsole.Error<CrossDatabaseTransactionAchieve<TDbKey>>($"【多库事务提交失败「{describe}」】发生异常，其他全部回滚.");
            }

            return false;
        }

        private (bool, Exception?) DeputyTransactionOutcomesTransactionCommit(string dbName,
            DbTransaction transaction)
        {
            try
            {
                transaction.Commit();
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return (false, ex);
            }
        }

        private bool _isRollback = false;

        public void Rollback()
        {
            foreach (var transactions in Transactions)
            {
                try
                {
                    transactions.DbTransaction.Rollback();
                }
                catch
                {
                    continue;
                }
            }

            _isRollback = true;
        }

        //析构函数
        ~CrossDatabaseTransactionAchieve() => Dispose();

        public void Dispose()
        {
            try
            {
                if (_isRollback == false) Rollback();

                foreach (var key in refers)
                {
                    try
                    {
                        var db = key.FreeSql;
                        //使用完毕归还资源
                        db.Ado.MasterPool.Return(_connections[key.Database]);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            finally
            {
                CrossDatabaseTransactionSqlLogger.Clear();
                _connections = null;
                Transactions = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}