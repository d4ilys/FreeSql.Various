using System.Collections.Concurrent;
using FreeSql.Various.Contexts;
using FreeSql.Various.Models;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.Sharing.Pattern;

/// <summary>
/// 时间范围分库模式
/// </summary>
/// <param name="schedule"></param>
/// <param name="tenantContext"></param>
/// <typeparam name="TDbKey"></typeparam>
public class TimeRangeSharingPattern<TDbKey>(FreeSqlSchedule schedule, VariousTenantContext tenantContext)
    where TDbKey : notnull
{
    private static readonly ConcurrentDictionary<TDbKey, TimeRangeShardingRegisterConfigure> Cache = new();

    /// <summary>
    /// 使用时间获取对应FreeSql操作对象
    /// </summary>
    /// <param name="dbKey">数据库键</param>
    /// <param name="inputName">输入名称</param>
    /// <returns></returns>
    public IFreeSql Use(TDbKey dbKey, DateTime inputName)
    {
        var dbName = GenerateDatabaseName(dbKey, inputName);

        var freeSql = schedule.Get(dbName);

        return freeSql;
    }

    /// <summary>
    /// 使用时间获取对应FreeSql操作包装对象
    /// </summary>
    /// <param name="dbKey">数据库键</param>
    /// <param name="inputName">输入名称</param>
    /// <returns></returns>
    public FreeSqlElaborate<TDbKey> UseElaborate(TDbKey dbKey, DateTime inputName)
    {
        var dbName = GenerateDatabaseName(dbKey, inputName);

        var freeSql = schedule.Get(dbName);

        return new FreeSqlElaborate<TDbKey>
        {
            DbKey = dbKey,
            FreeSql = freeSql,
            Database = dbName
        };
    }

    /// <summary>
    /// 使用时间范围获取对应FreeSql操作对象
    /// </summary>
    /// <param name="dbKey">数据库键</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    public IEnumerable<IFreeSql> Use(TDbKey dbKey, DateTime startTime, DateTime endTime)
    {
        var dbNames = GenerateDatabaseName(dbKey, startTime, endTime);

        foreach (var dbName in dbNames)
        {
            var freeSql = schedule.Get(dbName);
            yield return freeSql;
        }
    }


    /// <summary>
    /// 使用时间范围获取对应FreeSql操作对象
    /// </summary>
    /// <param name="dbKey">数据库键</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    public IEnumerable<FreeSqlElaborate<TDbKey>> UseElaborate(TDbKey dbKey, DateTime startTime, DateTime endTime)
    {
        var dbNames = GenerateDatabaseName(dbKey, startTime, endTime);

        foreach (var dbName in dbNames)
        {
            var freeSql = schedule.Get(dbName);
            yield return new FreeSqlElaborate<TDbKey>
            {
                DbKey = dbKey,
                FreeSql = freeSql,
                Database = dbName
            };
        }
    }

    /// <summary>
    /// 获取所有数据库
    /// </summary>
    /// <param name="dbKey"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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

    /// <summary>
    /// 注册数据库分片
    /// </summary>
    /// <param name="dbKey">数据库键</param>
    /// <param name="registerConfigure">注册配置</param>
    public void Register(TDbKey dbKey, TimeRangeShardingRegisterConfigure registerConfigure)
    {
        Cache.TryAdd(dbKey, registerConfigure);
        foreach (var item in registerConfigure.FreeSqlRegisterItems)
        {
            schedule.Register(item.Database, item.BuildIFreeSqlDelegate);
        }
    }

    /// <summary>
    /// 表格查询
    /// </summary>
    /// <param name="dbKey">指定数据库</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="noCrossDatabaseOperation">单库查询</param>
    /// <param name="crossDatabaseOperation">跨库查询</param>
    /// <remarks>
    /// 该操作一般用于表格查询，如果跨库不走数据库查询 <br/> 
    /// 建议使用 ElasticSearch Clickhouse  <br/> 
    /// 如果指定的日期范围不跨库，优先查询数据库数据  <br/> 
    /// </remarks>
    /// <returns></returns>
    public async Task<TableCrossDbQueryOutcome<TResult>> TableCrossDatabaseQueryAsync<TResult>(TDbKey dbKey,
        DateTime startTime,
        DateTime endTime,
        Func<IFreeSql, Task<TableCrossDbQueryOutcome<TResult>>> noCrossDatabaseOperation,
        Func<Task<TableCrossDbQueryOutcome<TResult>>> crossDatabaseOperation)
    {
        var dbNames = GenerateDatabaseName(dbKey, startTime, endTime).ToList();

        TableCrossDbQueryOutcome<TResult> operationOutcomes;

        if (dbNames.Count == 1)
        {
            // 单库查询
            var freeSql = schedule.Get(dbNames.First());
            operationOutcomes = await noCrossDatabaseOperation(freeSql);
        }
        else
        {
            // 跨库查询
            operationOutcomes = await crossDatabaseOperation();
        }

        return operationOutcomes;
    }

    /// <summary>
    /// 跨库并行操作
    /// </summary>
    /// <param name="dbKey">指定数据库</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="operation">操作委托</param>
    /// <returns></returns>
    public async Task<IList<CrossDatabaseOperationOutcome>> CrossDatabaseOperationAsync(TDbKey dbKey,
        DateTime startTime,
        DateTime endTime,
        Func<IFreeSql, Task> operation)
    {
        var dbNames = GenerateDatabaseName(dbKey, startTime, endTime);

        var operationResults = new List<CrossDatabaseOperationOutcome>();

        var tasks = dbNames.Select(name => Task.Run(async () =>
            {
                var operationResult = new CrossDatabaseOperationOutcome { DatabaseName = name, Success = false, };

                try
                {
                    var freeSql = schedule.Get(name);
                    await operation(freeSql);
                    operationResult.Success = true;
                }
                catch (Exception e)
                {
                    operationResult.ErrorMessage = e.ToString();
                }

                return operationResult;
            }))
            .ToList();

        // 等待所有任务完成
        await Task.WhenAll(tasks);

        // 收集任务结果
        foreach (var task in tasks)
        {
            operationResults.Add(await task);
        }

        operationResults.AddRange();

        return operationResults;
    }

    /// <summary>
    /// 跨库并行查询
    /// </summary>
    /// <param name="dbKey">指定数据库</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="operation">操作委托</param>
    /// <remarks>并行查询 返回结果无序 需自行排序</remarks>
    /// <returns></returns>
    public async Task<List<TResult>> CrossDatabaseQueryAsync<TResult>(TDbKey dbKey, DateTime startTime,
        DateTime endTime,
        Func<IFreeSql, Task<List<TResult>>> operation)
    {
        var dbNames = GenerateDatabaseName(dbKey, startTime, endTime);

        var operationResults = new List<TResult>();

        var tasks = dbNames.Select(name => Task.Run(async () =>
        {
            try
            {
                var freeSql = schedule.Get(name);
                return await operation(freeSql);
            }
            catch (Exception e)
            {
                VariousConsole.Error<TimeRangeSharingPattern<TDbKey>>($"[{name}]，跨库查询发生异：{e}");
                return [];
            }
        })).ToList();

        // 等待所有任务完成
        await Task.WhenAll(tasks);

        // 收集任务结果
        foreach (var task in tasks)
        {
            operationResults.AddRange(await task);
        }

        return operationResults;
    }

    private string GenerateDatabaseName(TDbKey dbKey, DateTime inputTime)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);
        if (!tryGetValue)
        {
            throw new ArgumentException($"未找到该数据库注册配置信息");
        }

        var currTenant = tenantContext.Get();

        var timeRangeShardingRegisterTenantConfigure =
            configure?.TenantConfigure.FirstOrDefault(t => t.TenantMark == currTenant);

        if (timeRangeShardingRegisterTenantConfigure == null)
        {
            throw new ArgumentException($"未找到该租户注册配置信息");
        }

        var range = TimeRangeCalculator.GetBelongTime(inputTime, timeRangeShardingRegisterTenantConfigure!.SharingStartTime, timeRangeShardingRegisterTenantConfigure.Period);

        var dbName = DatabaseNameTemplateReplacer.ReplaceTemplate(configure!.DatabaseNamingTemplate,
            new Dictionary<string, string>
            {
                { "tenant", currTenant },
                { "range", range }
            });

        return dbName;
    }

    private IEnumerable<string> GenerateDatabaseName(TDbKey dbKey, DateTime startTime, DateTime endTime)
    {
        var tryGetValue = Cache.TryGetValue(dbKey, out var configure);
        if (!tryGetValue)
        {
            throw new ArgumentException($"未找到该数据库注册配置信息");
        }
        var currTenant = tenantContext.Get();

        var timeRangeShardingRegisterTenantConfigure =
            configure?.TenantConfigure.FirstOrDefault(t => t.TenantMark == currTenant);

        if (timeRangeShardingRegisterTenantConfigure == null)
        {
            throw new ArgumentException($"未找到该租户注册配置信息");
        }

        var range = TimeRangeCalculator.GetBelongTimeRange(startTime, endTime, timeRangeShardingRegisterTenantConfigure!.SharingStartTime,
            timeRangeShardingRegisterTenantConfigure.Period);

     

        foreach (var time in range)
        {
            var dbName = DatabaseNameTemplateReplacer.ReplaceTemplate(configure.DatabaseNamingTemplate,
                new Dictionary<string, string>
                {
                    { "Tenant", currTenant },
                    { "Range", time }
                });

            yield return dbName;
        }
    }
}