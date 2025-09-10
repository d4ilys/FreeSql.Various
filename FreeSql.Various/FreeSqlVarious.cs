using FreeSql.Various.Contexts;
using FreeSql.Various.Models;
using FreeSql.Various.SeniorTransactions;
using FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility;
using FreeSql.Various.Sharing;
using FreeSql.Various.Utilitys;
using System;
using System.Collections.Concurrent;

namespace FreeSql.Various;

public class FreeSqlVarious<TDbKey> where TDbKey : notnull
{
    private readonly FreeSqlSchedule _schedule;

    private readonly VariousTenantContext _tenantContextContext;

    protected FreeSqlVarious()
    {
        _tenantContextContext = new VariousTenantContext();
        _schedule = new FreeSqlSchedule();
        SharingPatterns = new VariousSharingPatterns<TDbKey>(_schedule, _tenantContextContext);
        Transactions = new VariousTransactions<TDbKey>(_schedule);
    }

    /// <summary>
    /// 根据数据库枚举获取FreeSql对象
    /// </summary>    
    /// <param name="dbKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IFreeSql Use(TDbKey dbKey)
    {
        var name = dbKey.ToString();
        if (name != null) return _schedule.Get(name);
        throw new Exception($"该数据库[{dbKey}]未注册.");
    }

    /// <summary>
    /// 根据数据库名称获取FreeSql对象
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IFreeSql Use(string database)
    {
        if (!string.IsNullOrWhiteSpace(database)) return _schedule.Get(database);
        throw new Exception($"该数据库[{database}]未注册.");
    }

    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey)
    {
        var name = dbKey.ToString();
        if (name != null)
            return new FreeSqlElaborate<TDbKey>
            {
                DbKey = dbKey,
                Database = name,
                FreeSql = _schedule.Get(name)
            };
        throw new Exception($"该数据库[{dbKey}]未注册.");
    }

    public void Register(TDbKey dbKey, Func<IFreeSql> create)
    {
        var name = dbKey.ToString();
        if (name != null) _schedule.Register(name, create);
    }

    /// <summary>
    /// 租户上下文
    /// </summary>
    public VariousTenantContext TenantContext => _tenantContextContext;

    /// <summary>
    /// 分库模型
    /// </summary>
    public VariousSharingPatterns<TDbKey> SharingPatterns { get; private set; }

    /// <summary>
    /// 事务
    /// </summary>
    public VariousTransactions<TDbKey> Transactions { get; private set; }
}
