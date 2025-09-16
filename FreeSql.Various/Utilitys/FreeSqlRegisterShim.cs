using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.Aop;
using FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

namespace FreeSql.Various.Utilitys
{
    internal class FreeSqlRegisterShim
    {
        internal static IFreeSql Create(Func<IFreeSql> register)
        {
            var fsql = register();

            //增加Aop
            fsql.Aop.CurdAfter += (sender, args) =>
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
            };

            fsql.Aop.TraceAfter += (sender, args) =>
            {
                if (sender is IUnitOfWork uow &&
                    uow.States.TryGetValue("LocalMessageTableTransaction", out object? state))
                {
                    if (args.Remark == "提交")
                    {
                        if (state is LocalMessageTableTransactionUnitOfWorker worker)
                        {
                            _ = worker.DoAsync();
                        }
                    }
                }
            };

            return fsql;
        }
    }
}