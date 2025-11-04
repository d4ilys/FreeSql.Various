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
        var tenant = tenantContext.GetCurrent();
        return Use(dbKey, tenant);
    }

    public IFreeSql Use(TDbKey dbKey, string tenant)
    {
        var refer = UseElaborate(dbKey, tenant);
        return refer.FreeSql;
    }
    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey)
    {
        var tenant = tenantContext.GetCurrent();
        return UseElaborate(dbKey, tenant);
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

        var elaborate = schedule.Get(dbName);

        return new FreeSqlElaborate<TDbKey>
        {
            DbKey = dbKey,
            FreeSql = elaborate.FreeSql,
            Database = dbName
        };
    }

    public IEnumerable<IFreeSql> UseAll(TDbKey dbKey) =>  UseElaborateAll(dbKey).Select(elaborate => elaborate.FreeSql);

    public IEnumerable<FreeSqlElaborate<TDbKey>> UseElaborateAll(TDbKey dbKey)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);

        if (!tryGetValue)
        {
            throw new Exception($"未找到该数据库注册配置信息");
        }

        var keys = configure!.FreeSqlRegisterItems.Select(item => item.Database);

        foreach (string key in keys)
        {
            if (schedule.IsRegistered(key))
            {
                var elaborate = schedule.Get(key);
                yield return new FreeSqlElaborate<TDbKey>
                {
                    FreeSql = elaborate.FreeSql,
                    Database = elaborate.Database,
                    DbKey = dbKey
                };
            }
        }
    }

    public void Register(TDbKey dbKey, TenantSharingRegisterConfigure registerConfigure)
    {
        Cache.TryAdd(dbKey, registerConfigure);
        foreach (var item in registerConfigure.FreeSqlRegisterItems)
        {
            schedule.Register(item.Database, () =>
            {
                var freeSql = FreeSqlRegisterShim.Create(item.BuildIFreeSqlDelegate);
                return new FreeSqlElaborate
                {
                    FreeSql = freeSql,
                    Database = item.Database
                };
            });
        }
    }
}