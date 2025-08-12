using System.Collections.Concurrent;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;

/// <summary>
/// 本地消息表 + 定时任务调度实现最终一致性
/// </summary>
public class LocalMessageTableTransaction<TDbKey>(FreeSqlSchedule schedule)
{
    private static readonly ConcurrentDictionary<string, Func<string, Task<bool>>> _tasks = new();

    private static readonly HashSet<IFreeSql> _schedulerDbs = new HashSet<IFreeSql>();

    private static Timer _schedulerTimer;

    /// <summary>
    /// 启动数据库调度
    /// </summary>
    /// <param name="period">调度周期</param>
    /// <param name="idempotentReentrant">幂等/重入屏障</param>
    /// <param name="errorMessageNotice">消息通知</param>
    public void StartScheduler(TimeSpan period, Func<Func<Func<string, Task>, Task>, Task>? idempotentReentrant = null,
        Func<string, Task>? errorMessageNotice = null
    )
    {
        _schedulerTimer = new Timer(_ => idempotentReentrant?.Invoke(ScheduleAction), null, period, period);
    }

    private async Task ScheduleAction(Func<string, Task>? errorMessageNotice = null)
    {
        foreach (var db in _schedulerDbs)
        {
            try
            {
                //获取小于当前一分钟内的消息，防止添加成功后立即执行
                var messageStartTime = DateTime.Now.AddMinutes(-1);
                var finalConsistencyMessages = await db.Select<FinalConsistencyMessage>()
                    .Where(m => m.MessageTime < messageStartTime).ToListAsync();

                foreach (var message in finalConsistencyMessages)
                {
                    if (message.Retries < 20)
                    {
                        var achieve = new LocalMessageTableTransactionUnitOfWorker(_tasks, schedule);
                        _ = achieve.ScheduleDoAsync(message.TaskKey, message.MessageContent, db);
                    }
                    else
                    {
                        //超过30分钟了
                        if (DateTime.Now - message.MessageTime > TimeSpan.FromMinutes(30))
                        {
                            errorMessageNotice?.Invoke($"【本地消息表事务】「{message.TaskKey}」执行超过最大次数20次.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleHelper.Error<LocalMessageTableTransaction<TDbKey>>($"【本地消息表事务】调度任务异常：{e.Message}");
            }
        }
    }

    /// <summary>
    /// 注册需要调度的数据库
    /// </summary>
    public void RegisterScheduleDatabase(params IFreeSql[] dbs)
    {
        _schedulerDbs.UnionWith(dbs);
    }

    /// <summary>
    /// 注册延续任务
    /// </summary>
    /// <param name="taskKey"></param>
    /// <param name="execute"></param>
    public void RegisterTaskExecutor(string taskKey, Func<string, Task<bool>> execute)
    {
        _tasks.TryAdd(taskKey, execute);
    }

    /// <summary>
    /// 创建本地消息事务
    /// </summary>
    /// <returns></returns>
    public LocalMessageTableTransactionUnitOfWorker CreateUnitOfWorker()
    {
        return new LocalMessageTableTransactionUnitOfWorker(_tasks, schedule);
    }
}