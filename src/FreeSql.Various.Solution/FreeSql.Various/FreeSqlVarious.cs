using FreeSql.Various.Contexts;
using FreeSql.Various.Models;
using FreeSql.Various.SeniorTransactions;
using FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility;
using FreeSql.Various.Sharing;
using FreeSql.Various.Utilitys;
using System;
using System.Collections.Concurrent;
using FreeSql.Various.Dashboard;

namespace FreeSql.Various;

public class FreeSqlVarious<TDbKey> where TDbKey : notnull
{
    protected FreeSqlVarious()
    {
        TenantContext = new VariousTenantContext();
        Schedule = new FreeSqlSchedule();
        Dashboard = new VariousDashboard();
        SharingPatterns = new VariousSharingPatterns<TDbKey>(this);
        SeniorTransactions = new VariousSeniorTransactions<TDbKey>(this);
    }

    /// <summary>
    /// 举获取FreeSql对象
    /// </summary>    
    /// <param name="dbKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IFreeSql Use(TDbKey dbKey)
    {
        var name = dbKey.ToString();
        if (name != null) return Schedule.Get(name).FreeSql;
        throw new Exception($"该数据库[{dbKey}]未注册.");
    }

    /// <summary>
    /// 获取FreeSql对象
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IFreeSql Use(string database)
    {
        if (!string.IsNullOrWhiteSpace(database)) return Schedule.Get(database).FreeSql;
        throw new Exception($"该数据库[{database}]未注册.");
    }

    /// <summary>
    /// 获取FreeSql描述对象
    /// </summary>
    /// <param name="dbKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey)
    {
        var name = dbKey.ToString();
        if (name != null)
        {
            var ela = Schedule.Get(name);
            return new FreeSqlElaborate<TDbKey>
            {
                DbKey = dbKey,
                Database = ela.Database,
                FreeSql = ela.FreeSql
            };
        }

        throw new Exception($"该数据库[{dbKey}]未注册.");
    }

    /// <summary>
    /// 注册数据库
    /// </summary>
    /// <param name="dbKey"></param>
    /// <param name="create"></param>
    public void Register(TDbKey dbKey, Func<IFreeSql> create)
    {
        var name = dbKey.ToString();
        if (name != null)
            Schedule.Register(name, () =>
            {
                var freeSql = FreeSqlRegisterShim.Create(create);
                return new FreeSqlElaborate
                {
                    FreeSql = freeSql,
                    Database = name
                };
            });
    }

    /// <summary>
    /// 租户上下文
    /// </summary>
    public VariousTenantContext TenantContext;

    /// <summary>
    /// 分库模型
    /// </summary>
    public VariousSharingPatterns<TDbKey> SharingPatterns { get; private set; }

    /// <summary>
    /// 高级事务
    /// </summary>
    public VariousSeniorTransactions<TDbKey> SeniorTransactions { get; private set; }

    /// <summary>
    /// FreeSql调度器
    /// </summary>
    public FreeSqlSchedule Schedule { get; }

    /// <summary>
    /// 仪表盘
    /// </summary>
    public VariousDashboard Dashboard { get; }
}