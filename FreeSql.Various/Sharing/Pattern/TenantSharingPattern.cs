using System.Collections.Concurrent;
using FreeSql.Various.Contexts;
using FreeSql.Various.Utilitys;

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
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);
        if (!tryGetValue)
        {
            throw new Exception($"未找到该数据库注册配置信息");
        }

        var dbName = DatabaseNameTemplateReplacer.ReplaceTemplate(configure!.DatabaseNamingTemplate,
            new Dictionary<string, string>
            {
                { "Tenant", tenant }
            });

        var freeSql = schedule.Get(dbName);

        return freeSql;
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