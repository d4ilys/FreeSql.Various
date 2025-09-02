using FreeSql.Various.Contexts;
using FreeSql.Various.Models;
using FreeSql.Various.Utilitys;
using System.Collections.Concurrent;

namespace FreeSql.Various.Sharing.Pattern;

public class TenantSharingPattern<TDbKey>(FreeSqlSchedule schedule, VariousTenantContext tenantContext)
    where TDbKey : notnull
{
    private static readonly ConcurrentDictionary<TDbKey, TenantSharingRegisterConfigure> Cache = new();

    public IFreeSql Use(TDbKey dbKey)
    {
        var tenant = tenantContext.Get();
        return Use(dbKey, tenant);
    }

    public IFreeSql Use(TDbKey dbKey, string tenant)
    {
        var refer = UseElaborate(dbKey, tenant);
        return refer.FreeSql;
    }

    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey, string tenant)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);
        if (!tryGetValue)
        {
            throw new Exception($"未找到该数据库注册配置信息");
        }

        var dbName = DatabaseNameTemplateReplacer.ReplaceTemplate(configure!.DatabaseNamingTemplate,
            new Dictionary<string, string>
            {
                { "tenant", tenant }
            });

        var freeSql = schedule.Get(dbName);

        return new FreeSqlElaborate<TDbKey>
        {
            DbKey = dbKey,
            FreeSql = freeSql,
            Database = dbName
        };
    }

    public IEnumerable<IFreeSql> UseAll(TDbKey dbKey)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);

        if (!tryGetValue)
        {
            throw new Exception($"未找到该数据库注册配置信息");
        }

        var dbs = configure!.FreeSqlRegisterItems.Select(item => item.Database);

        foreach (string db in dbs)
        {
            if (schedule.IsRegistered(db))
            {
                yield return schedule.Get(db);
            }
        }
    }
    
    public void Register(TDbKey dbKey, TenantSharingRegisterConfigure registerConfigure)
    {
        Cache.TryAdd(dbKey, registerConfigure);
        foreach (var item in registerConfigure.FreeSqlRegisterItems)
        {
            schedule.Register(item.Database, item.BuildIFreeSqlDelegate);
        }
    }
}