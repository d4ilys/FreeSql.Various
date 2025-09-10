using System.Collections.Concurrent;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

/// <summary>
/// 本地消息表 + 定时任务调度实现最终一致性
/// </summary>
public class LocalMessageTableTransaction<TDbKey>(FreeSqlSchedule schedule)
{
    private static Timer _schedulerTimer;

    private Func<string, Task>? _errorMessageNotice = null;

    private int _maxRetries;

    /// <summary>
    /// 启动数据库调度
    /// </summary>
    /// <param name="period">调度周期</param>
    /// <param name="dueTime">第一次执行时间</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="idempotentReentrant">幂等/重入屏障</param>
    public void StartScheduler(TimeSpan period, TimeSpan dueTime, int maxRetries = 20,
        Func<Func<Task>, Task>? idempotentReentrant = null
    )
    {
        _maxRetries = maxRetries;
        _schedulerTimer = new Timer(o =>
        {
            if (idempotentReentrant != null)
            {
                idempotentReentrant?.Invoke(ScheduleAction);
            }
            else
            {
                _ = ScheduleAction();
            }
        }, null, dueTime, period);
    }

    private async Task ScheduleAction()
    {
        foreach (var db in VariousMemoryCache.LocalMessageTableTransactionSchedulerDbs)
        {
            try
            {
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddMinutes(-1);

                //执行的时候同步一次本地消息表
                var syncResult = VariousMemoryCache.LazySyncLocalMessageTable.GetOrAdd(
                    db.Ado.ConnectionString.GetHashCode(),
                    new Lazy<bool>(() =>
                    {
                        try
                        {
                            db.CodeFirst.SyncStructure<LocalMessageTable>();
                        }
                        catch (Exception e)
                        {
                            VariousConsole.Error<LocalMessageTableTransaction<TDbKey>>(
                                $"同步本地消息表失败「{db.Ado.ConnectionString}」,「Exception」: {e}");
                            return false;
                        }

                        return true;
                    }));

                if (syncResult.Value == false) break;

                var finalConsistencyMessages = await db.Select<LocalMessageTable>()
                    .Where(m => m.MessageTime < messageStartTime)
                    .ToListAsync();

                _ = Parallel.ForEachAsync(
                    finalConsistencyMessages.ToLookup(f => new { f.Group, f.GroupEnsureOrderliness }),
                    new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (messages, token) =>
                    {
                        bool groupEnsureOrderliness = messages.Key.GroupEnsureOrderliness;
                        foreach (var message in messages)
                        {
                            if (groupEnsureOrderliness)
                            {
                                //如果该组的ensureOrderliness为true，则需要确保该组内的消息顺序
                                var res = await GroupEnsureOrderlinessLogicAsync(message, db);
                                if (!res) break;
                            }
                            else
                            {
                                NormalGroupLogic(message, db);
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

    private async Task<bool> GroupEnsureOrderlinessLogicAsync(LocalMessageTable messageTable, IFreeSql db)
    {
        if (messageTable.Retries < _maxRetries)
        {
            var achieve = new LocalMessageTableTransactionUnitOfWorker(
                VariousMemoryCache.LocalMessageTableTransactionTasks, schedule);
            var res = await achieve.ScheduleDoAsync(messageTable.TaskKey, messageTable.MessageContent, db);
            return res;
        }
        else
        {
            //超过30分钟了
            if (DateTime.Now - messageTable.MessageTime > TimeSpan.FromMinutes(30))
            {
                _errorMessageNotice?.Invoke($"【本地消息表事务】「{messageTable.TaskKey}」执行超过最大次数20次.");
            }
        }

        return false;
    }

    private void NormalGroupLogic(LocalMessageTable messageTable, IFreeSql db)
    {
        if (messageTable.Retries < _maxRetries)
        {
            var achieve = new LocalMessageTableTransactionUnitOfWorker(VariousMemoryCache.LocalMessageTableTransactionTasks, schedule);
            _ = achieve.ScheduleDoAsync(messageTable.TaskKey, messageTable.MessageContent, db);
        }
        else
        {
            //超过30分钟了
            if (DateTime.Now - messageTable.MessageTime > TimeSpan.FromMinutes(30))
            {
                _errorMessageNotice?.Invoke($"【本地消息表事务】「{messageTable.TaskKey}」执行超过最大次数20次.");
            }
        }
    }

    /// <summary>
    /// 注册需要调度的数据库
    /// </summary>
    public void RegisterScheduleDatabase(params IFreeSql[] dbs)
    {
        VariousMemoryCache.LocalMessageTableTransactionSchedulerDbs.UnionWith(dbs);
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

    public void RegisterErrorMessageNotice(Func<string, Task> errorMessageNotice)
    {
        _errorMessageNotice = errorMessageNotice;
    }

    /// <summary>
    /// 创建本地消息事务
    /// </summary>
    /// <returns></returns>
    public LocalMessageTableTransactionUnitOfWorker CreateUnitOfWorker()
    {
        return new LocalMessageTableTransactionUnitOfWorker(VariousMemoryCache.LocalMessageTableTransactionTasks, schedule);
    }
}