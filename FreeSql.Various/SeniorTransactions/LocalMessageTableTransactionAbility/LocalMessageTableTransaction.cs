using System.Collections.Concurrent;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

/// <summary>
/// 本地消息表 + 定时任务调度实现最终一致性
/// </summary>
public class LocalMessageTableTransaction<TDbKey>(FreeSqlSchedule schedule)
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

        //启动分组调度
        foreach (var localMessageTableGroupDispatchSchedule in _dispatchConfig.GroupSchedules)
        {
            var currentSchedule = localMessageTableGroupDispatchSchedule;

            var groupSchedulerTimer = new Timer(o =>
                {
                    if (_dispatchConfig.IdempotentReentrant != null)
                    {
                        _dispatchConfig.IdempotentReentrant?.Invoke(() => GroupDispatchAction(currentSchedule.Key));
                    }
                    else
                    {
                        _ = GroupDispatchAction(currentSchedule.Key);
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
            var db = schedule.Get(key);
            SyncDatabaseLocalMessageTable(db);
        }
    }

    public void SyncDatabaseLocalMessageTable(IFreeSql db)
    {
        db.CodeFirst.SyncStructure<LocalMessageDatabaseTableLogger>();
        db.CodeFirst.SyncStructure<LocalMessageGroupDatabaseTable>();
        db.CodeFirst.SyncStructure<LocalMessageDatabaseTable>();
        VariousMemoryCache.IsSyncLocalMessageTable.TryAdd(db.Ado.ConnectionString.GetHashCode(), true);
    }

    /// <summary>
    /// 注册需要调度的数据库
    /// </summary>
    public void RegisterDispatchDatabase(params IFreeSql[] dbs)
    {
        foreach (var freeSql in dbs)
        {
            var keys = schedule.GetIdleBus().GetKeys(fsql =>
            {
                if (fsql == null)
                {
                    return false;
                }

                return fsql == freeSql;
            });

            VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys.UnionWith(keys);
        }
    }

    public void ConfigDispatch(Action<LocalMessageTableDispatchConfig> configAction)
    {
        _dispatchConfig = new LocalMessageTableDispatchConfig();
        configAction(_dispatchConfig);
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
            schedule, _dispatchConfig!);
    }

    private async Task MainDispatchAction()
    {
        foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
        {
            try
            {
                var db = schedule.Get(key);
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddSeconds(-20);

                if (LazySyncLocalMessageTable(db) == false) break;

                var finalConsistencyMessages = await db.Select<LocalMessageDatabaseTable>()
                    .Where(m => m.MessageTime < messageStartTime)
                    .OrderBy(m => m.MessageTime)
                    .ToListAsync();

                foreach (var localMessageDatabaseTable in finalConsistencyMessages)
                {
                    _ = NormalGroupLogicAsync(localMessageDatabaseTable, string.Empty, db);
                }
            }
            catch (Exception e)
            {
                VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】调度任务异常：{e.Message}");
            }
        }
    }

    private async Task GroupDispatchAction(string group)
    {
        foreach (var key in VariousMemoryCache.LocalMessageTableTransactionSchedulerKeys)
        {
            try
            {
                var db = schedule.Get(key);
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddSeconds(-20);

                //执行的时候同步一次本地消息表
                if (LazySyncLocalMessageTable(db) == false) break;

                var finalConsistencyMessages = await db.Select<LocalMessageGroupDatabaseTable>()
                    .Where(m => m.MessageTime < messageStartTime)
                    .Where(m => m.Group == group)
                    .OrderBy(m => m.MessageTime)
                    .ToListAsync();

                _dispatchConfig!.GroupSchedules.TryGetValue(group, out var groupDispatchSchedule);

                if (groupDispatchSchedule?.GroupEnsureOrderliness ?? false)
                {
                    foreach (var message in finalConsistencyMessages)
                    {
                        //如果该组的ensureOrderliness为true，则需要确保该组内的消息顺序
                        var res = await GroupEnsureOrderlinessLogicAsync(message, group, db,
                            groupDispatchSchedule.Schedule.MaxRetries);
                        if (!res) break;
                    }
                }
                else
                {
                    foreach (var message in finalConsistencyMessages)
                    {
                        await NormalGroupLogicAsync(message, group, db);
                    }
                }
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
        string group,
        IFreeSql db, int maxRetries)
    {
        try
        {
            if (messageGroupDatabaseTable.Retries <= maxRetries)
            {
                var achieve = new LocalMessageTableTransactionUnitOfWorker(
                    VariousMemoryCache.LocalMessageTableTransactionTasks, schedule, _dispatchConfig!);
                var res = await achieve.ScheduleDoAsync(messageGroupDatabaseTable.Id, messageGroupDatabaseTable.TaskKey,
                    messageGroupDatabaseTable.MessageContent, group, db);

                if (!res)
                {
                    if (string.IsNullOrEmpty(group))
                    {
                        await db.Update<LocalMessageDatabaseTable>().Set(t => t.Retries + 1)
                            .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                    }
                    else
                    {
                        await db.Update<LocalMessageGroupDatabaseTable>().Set(t => t.Retries + 1)
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

    private async Task NormalGroupLogicAsync(LocalMessageDatabaseTable messageGroupDatabaseTable, string group,
        IFreeSql db)
    {
        try
        {
            if (messageGroupDatabaseTable.Retries <= _dispatchConfig!.MainSchedule.MaxRetries)
            {
                var achieve =
                    new LocalMessageTableTransactionUnitOfWorker(VariousMemoryCache.LocalMessageTableTransactionTasks,
                        schedule, _dispatchConfig!);

                _ = achieve.ScheduleDoAsync(messageGroupDatabaseTable.Id, messageGroupDatabaseTable.TaskKey,
                    messageGroupDatabaseTable.MessageContent, group,
                    db);

                if (string.IsNullOrEmpty(group))
                {
                    await db.Update<LocalMessageDatabaseTable>().Set(t => t.Retries + 1)
                        .Where(l => l.Id == messageGroupDatabaseTable.Id).ExecuteAffrowsAsync();
                }
                else
                {
                    await db.Update<LocalMessageGroupDatabaseTable>().Set(t => t.Retries + 1)
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
                    var db = schedule.Get(key);
                    db.Delete<LocalMessageDatabaseTableLogger>()
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