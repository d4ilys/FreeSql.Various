using System.Collections.Concurrent;
using FreeSql.Various.Contexts;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.Sharing.Pattern;

public class TimeRangeSharingPattern<TDbKey>(FreeSqlSchedule schedule, VariousTenantContext tenantContext)
    where TDbKey : notnull
{
    private static readonly ConcurrentDictionary<TDbKey, TimeRangeShardingRegisterConfigure> Cache = new();

    public IFreeSql Use(TDbKey dbKey, DateTime inputName)
    {
        var dbName = GenerateDatabaseName(dbKey, inputName);

        //根据分库名称获取FreeSql实例
        var freeSql = schedule.Get(dbName);

        return freeSql;
    }

    public void Register(TDbKey dbKey, TimeRangeShardingRegisterConfigure registerConfigure)
    {
        Cache.TryAdd(dbKey, registerConfigure);
        foreach (var item in registerConfigure.FreeSqlRegisterItems)
        {
            schedule.Register(item.Database, item.BuildIFreeSqlDelegate);
        }
    }

    public string GenerateDatabaseName(TDbKey dbKey, DateTime inputTime)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);
        if (!tryGetValue)
        {
            throw new ArgumentException($"未找到该数据库注册配置信息");
        }

        var range = TimeRangeCalculator.GetTimeString(inputTime, configure!.StartTime, configure.Period);

        var tenant = string.Empty;

        if (configure.IsTenant)
        {
            tenant = tenantContext.Get();
        }

        var dbName = DatabaseNameTemplateReplacer.ReplaceTemplate(configure.DatabaseNamingTemplate,
            new Dictionary<string, string>
            {
                { "Tenant", tenant },
                { "Range", range }
            });

        return dbName;
    }
}