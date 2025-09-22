using System.Collections.Concurrent;
using FreeSql.Various.Contexts;
using FreeSql.Various.Models;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.Sharing.Pattern;

/// <summary>
/// 哈希分片分库模式
/// </summary>
/// <param name="schedule"></param>
/// <param name="tenantContext"></param>
/// <typeparam name="TDbKey"></typeparam>
public class HashSharingPattern<TDbKey>(FreeSqlSchedule schedule, VariousTenantContext tenantContext)
    where TDbKey : notnull
{
    private static readonly ConcurrentDictionary<TDbKey, HashShardingRegisterConfigure> Cache = new();

    /// <summary>
    /// 根据分片键获取FreeSql对象
    /// </summary>
    /// <param name="dbKey"></param>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IFreeSql Use(TDbKey dbKey, string partitionKey)
    {
        var refer = UseElaborate(dbKey, partitionKey);
        return refer.FreeSql;
    }

    /// <summary>
    /// 获取包装对象
    /// </summary>
    /// <param name="dbKey"></param>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentException"></exception>
    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey, string partitionKey)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);

        if (!tryGetValue)
        {
            throw new Exception($"未找到该数据库注册配置信息");
        }


        var currTenant = tenantContext.GetCurrent();

        var tenantConfigure =
            configure?.TenantConfigure.FirstOrDefault(t => t.TenantMark == currTenant);

        if (tenantConfigure == null)
        {
            throw new ArgumentException($"未找到该租户注册配置信息");
        }

        var locationShard = GetSlice(partitionKey, tenantConfigure);

        var key = DatabaseNameTemplateReplacer.ReplaceTemplate(configure!.DatabaseNamingTemplate,
            new Dictionary<string, string>
            {
                { "tenant", currTenant },
                { "slice", locationShard.ToString() }
            });

        var elaborate = schedule.Get(key);

        return new FreeSqlElaborate<TDbKey>
        {
            DbKey = dbKey,
            Database = key,
            FreeSql = elaborate.FreeSql
        };
    }

    /// <summary>
    /// 获取所有分库包装对象
    /// </summary>
    /// <param name="dbKey"></param>
    /// <returns></returns>
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
            if (!schedule.IsRegistered(key)) continue;

            var ela = schedule.Get(key);

            yield return new FreeSqlElaborate<TDbKey>
            {
                FreeSql = ela.FreeSql,
                Database = ela.Database,
                DbKey = dbKey
            };
        }
    }

    /// <summary>
    /// 获取所有分库
    /// </summary>
    /// <param name="dbKey"></param>
    /// <returns></returns>
    public IEnumerable<IFreeSql> UseAll(TDbKey dbKey) => UseElaborateAll(dbKey).Select(elaborate => elaborate.FreeSql);

    /// <summary>
    /// 注册数据库分片
    /// </summary>
    /// <param name="dbKey"></param>
    /// <param name="registerConfigure"></param>
    public void Register(TDbKey dbKey, HashShardingRegisterConfigure registerConfigure)
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

    private int GetSlice(string partitionKey, HashShardingRegisterTenantConfigure tenantConfigure)
    {
        //绝对正整数
        var hash = Math.Abs(partitionKey.GetHashCode());
        //取模
        var locationShard = hash % tenantConfigure!.Size;

        return locationShard + 1;
    }
}