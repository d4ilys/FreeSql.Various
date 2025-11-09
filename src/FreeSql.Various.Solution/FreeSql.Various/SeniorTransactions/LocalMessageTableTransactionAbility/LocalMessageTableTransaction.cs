using System.Collections.Concurrent;
using FreeSql.Various.Models;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

/// <summary>
/// 本地消息表 + 定时任务调度实现最终一致性
/// </summary>
public class LocalMessageTableTransaction<TDbKey>(FreeSqlVarious<TDbKey> various) where TDbKey : notnull
{
    private LocalMessageTableDispatchConfig? _dispatchConfig = null;

    /// <summary>
    /// 启动数据库调度
    /// </summary>
    public void DispatchRunning()
    {
        if (_dispatchConfig == null)
            throw new Exception("请先调用ConfigDispatch方法进行配置");

        var mainSchedulerTimer = new Timer(o =>
        {
            if (_dispatchConfig.IdempotentReentrant != null)
            {
                _dispatchConfig.IdempotentReentrant?.Invoke(MainDispatchAction);
            }
            else
            {
                _ = MainDispatchAction();
            }
        }, null, _dispatchConfig.MainSchedule.DueTime, _dispatchConfig.MainSchedule.Period);

        VariousMemoryCache.SchedulerTimers.Add(mainSchedulerTimer);

        //启动其他调度
        foreach (var governing in _dispatchConfig.GoverningSchedules)
        {
            var currentSchedule = governing;

            var groupSchedulerTimer = new Timer(o =>
                {
                    if (_dispatchConfig.IdempotentReentrant != null)
                    {
                        _dispatchConfig.IdempotentReentrant?.Invoke(() => GoverningDispatchAction(currentSchedule.Key));
                    }
                    else
                    {
                        _ = GoverningDispatchAction(currentSchedule.Key);
                    }
                }, null,
                currentSchedule.Value.Schedule.DueTime,
                currentSchedule.Value.Schedule.Period);
            VariousMemoryCache.SchedulerTimers.Add(groupSchedulerTimer);
        }

        //只保留三天的日志
        ClearLog();
    }

    public void SyncAllDatabaseLocalMessageTable()
    {
        foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
        {
            var elaborate = various.Schedule.Get(key);
            SyncDatabaseLocalMessageTable(elaborate.FreeSql);
        }
    }

    public void SyncDatabaseLocalMessageTable(IFreeSql db)
    {
        db.CodeFirst.SyncStructure<LocalMessageDatabaseTableLogger>();
        db.CodeFirst.SyncStructure<LocalMessageGoverningDatabaseTable>();
        db.CodeFirst.SyncStructure<LocalMessageDatabaseTable>();
        VariousMemoryCache.IsSyncLocalMessageTable.TryAdd(db.Ado.ConnectionString.GetHashCode(), true);
    }

    /// <summary>
    /// 注册需要调度的数据库
    /// </summary>
    public void RegisterDispatchDatabase(params FreeSqlElaborate<TDbKey>[] elaborates)
    {
        foreach (var ela in elaborates)
        {
            VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys.Add(ela.Database);
        }
    }

    public void ConfigDispatch(Action<LocalMessageTableDispatchConfig> configAction)
    {
        _dispatchConfig = new LocalMessageTableDispatchConfig();
        configAction(_dispatchConfig);
        VariousMemoryCache.LocalMessageTableDispatchConfig = _dispatchConfig;
    }

    /// <summary>
    /// 注册延续任务
    /// </summary>
    /// <param name="taskKey"></param>
    /// <param name="describe"></param>
    /// <param name="execute"></param>
    public void RegisterTaskExecutor(string taskKey, string describe, Func<string, Task<bool>> execute)
    {
        VariousMemoryCache.LocalMessageTableTaskDescribe[taskKey] = describe;
        VariousMemoryCache.LocalMessageTableTransactionTasks.TryAdd(taskKey, execute);
    }

    /// <summary>
    /// 创建本地消息事务
    /// </summary>
    /// <returns></returns>
    public LocalMessageTableTransactionUnitOfWorker CreateUnitOfWorker()
    {
        return new LocalMessageTableTransactionUnitOfWorker(VariousMemoryCache.LocalMessageTableTransactionTasks,
            various.Schedule, _dispatchConfig!, various.TenantContext);
    }

    private async Task MainDispatchAction()
    {
        foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
        {
            try
            {
                var elaborate = various.Schedule.Get(key);
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddSeconds(-20);

                if (LazySyncLocalMessageTable(elaborate.FreeSql) == false) break;

                var finalConsistencyMessages = await elaborate.FreeSql.Select<LocalMessageDatabaseTable>()
                    .Where(m => m.MessageTime < messageStartTime)
                    .OrderBy(m => m.MessageTime)
                    .ToListAsync();

                foreach (var localMessageDatabaseTable in finalConsistencyMessages)
                {
                    _ = NormalGroupLogicAsync(localMessageDatabaseTable, string.Empty, elaborate.FreeSql);
                }
            }
            catch (Exception e)
            {
                VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】调度任务异常：{e.Message}");
            }
        }
    }

    private async Task GoverningDispatchAction(string governing)
    {
        foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
        {
            try
            {
                var elaborate = various.Schedule.Get(key);
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddSeconds(-20);

                //执行的时候同步一次本地消息表
                if (LazySyncLocalMessageTable(elaborate.FreeSql) == false) break;

                var messages = await elaborate.FreeSql.Select<LocalMessageGoverningDatabaseTable>()
                    .Where(m => m.MessageTime < messageStartTime)
                    .Where(m => m.Governing == governing)
                    .ToListAsync();

                var tryGetValue =
                    _dispatchConfig!.GoverningSchedules.TryGetValue(governing, out var groupDispatchSchedule);

                if (!tryGetValue)
                {
                    throw new Exception($"没有配置Governing:{governing}");
                }

                //分组调度
                _ = Parallel.ForEachAsync(messages.ToLookup(t => t.Group),
                    new ParallelOptions() { MaxDegreeOfParallelism = 10 }, async (groupMessages, _) =>
                    {
                        var ensureOrder =
                            groupDispatchSchedule!.GroupEnsureOrderliness.GetValueOrDefault(groupMessages.Key);
                        if (ensureOrder)
                        {
                            foreach (var message in groupMessages)
                            {
                                //如果该组的ensureOrderliness为true，则需要确保该组内的消息顺序
                                var res = await GroupEnsureOrderlinessLogicAsync(message, groupMessages.Key,
                                    elaborate.FreeSql,
                                    groupDispatchSchedule!.Schedule.MaxRetries);
                                if (!res) break;
                            }
                        }
                        else
                        {
                            foreach (var message in messages)
                            {
                                await NormalGroupLogicAsync(message, governing, elaborate.FreeSql);
                            }
                        }
                    });
            }
            catch (Exception e)
            {
                VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】调度任务异常：{e.Message}");
            }
        }
    }

    private bool LazySyncLocalMessageTable(IFreeSql db)
    {
        //执行的时候同步一次本地消息表

        var syncResult = VariousMemoryCache.LazySyncLocalMessageTable.GetOrAdd(
            db.Ado.Identifier,
            new Lazy<bool>(() =>
            {
                if (VariousMemoryCache.IsSyncLocalMessageTable.TryGetValue(db.Ado.ConnectionString.GetHashCode(),
                        out bool value) && value)
                {
                    return true;
                }

                try
                {
                    SyncDatabaseLocalMessageTable(db);
                }
                catch (Exception e)
                {
                    VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>(
                        $"同步本地消息表失败「{db.Ado.ConnectionString}」,「Exception」: {e}");
                    return false;
                }

                return true;
            }));
        return syncResult.Value;
    }

    private async Task<bool> GroupEnsureOrderlinessLogicAsync(LocalMessageDatabaseTable messageGroupDatabaseTable,
        string governing,
        IFreeSql db, int maxRetries)
    {
        try
        {
            if (messageGroupDatabaseTable.Retries <= maxRetries)
            {
                var achieve = new LocalMessageTableTransactionUnitOfWorker(
                    VariousMemoryCache.LocalMessageTableTransactionTasks, various.Schedule, _dispatchConfig!,
                    various.TenantContext);

                var res = await achieve.ScheduleDoAsync(messageGroupDatabaseTable.Id, messageGroupDatabaseTable.TaskKey,
                    messageGroupDatabaseTable.MessageContent, governing, db, messageGroupDatabaseTable.TenantMark);

                if (!res)
                {
                    if (string.IsNullOrEmpty(governing))
                    {
                        await db.Update<LocalMessageDatabaseTable>().Set(t => t.Retries + 1)
                            .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                    }
                    else
                    {
                        await db.Update<LocalMessageGoverningDatabaseTable>().Set(t => t.Retries + 1)
                            .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                    }
                }

                await db.Update<LocalMessageDatabaseTableLogger>().Set(t => t.ExecutionCount + 1)
                    .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();

                return res;
            }
            else
            {
                //超过30分钟了
                if (DateTime.Now - messageGroupDatabaseTable.MessageTime > TimeSpan.FromMinutes(30))
                {
                    _dispatchConfig!.ErrorMessageNotice?.Invoke(
                        $"【本地消息表事务】「{messageGroupDatabaseTable.TaskKey}」执行超过最大次数{maxRetries}次.");
                }
            }
        }
        catch (Exception e)
        {
            VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>(
                $"【本地消息表事务】GroupEnsureOrderlinessLogicAsync调度任务异常：{e.Message}");
            return false;
        }

        return false;
    }

    private async Task NormalGroupLogicAsync(LocalMessageDatabaseTable messageGroupDatabaseTable, string governing,
        IFreeSql db)
    {
        try
        {
            if (messageGroupDatabaseTable.Retries <= _dispatchConfig!.MainSchedule.MaxRetries)
            {
                var achieve =
                    new LocalMessageTableTransactionUnitOfWorker(VariousMemoryCache.LocalMessageTableTransactionTasks,
                        various.Schedule, _dispatchConfig, various.TenantContext);

                _ = achieve.ScheduleDoAsync(messageGroupDatabaseTable.Id, messageGroupDatabaseTable.TaskKey,
                    messageGroupDatabaseTable.MessageContent, governing,
                    db, messageGroupDatabaseTable.TenantMark);

                if (string.IsNullOrEmpty(governing))
                {
                    await db.Update<LocalMessageDatabaseTable>().Set(t => t.Retries + 1)
                        .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                }
                else
                {
                    await db.Update<LocalMessageGoverningDatabaseTable>().Set(t => t.Retries + 1)
                        .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                }

                await db.Update<LocalMessageDatabaseTableLogger>().Set(t => t.ExecutionCount + 1)
                    .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
            }
            else
            {
                //超过30分钟了
                if (DateTime.Now - messageGroupDatabaseTable.MessageTime > TimeSpan.FromMinutes(30))
                {
                    _dispatchConfig!.ErrorMessageNotice?.Invoke(
                        $"【本地消息表事务】「{messageGroupDatabaseTable.TaskKey}」执行超过最大次数20次.");
                }
            }
        }
        catch (Exception e)
        {
            VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】NormalGroupLogic调度任务异常：{e.Message}");
        }
    }

    private void ClearLog()
    {
        var clearLogTimer = new Timer(o =>
        {
            foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
            {
                try
                {
                    var elaborate = various.Schedule.Get(key);
                    elaborate.FreeSql.Delete<LocalMessageDatabaseTableLogger>()
                        .Where(m => m.MessageTime < DateTime.Now.AddDays(-3))
                        .ExecuteAffrows();
                }
                catch (Exception e)
                {
                    VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】清除日志异常：{e.Message}");
                }
            }
        }, null, TimeSpan.FromHours(20), TimeSpan.FromHours(20));

        VariousMemoryCache.SchedulerTimers.Add(clearLogTimer);
    }
}