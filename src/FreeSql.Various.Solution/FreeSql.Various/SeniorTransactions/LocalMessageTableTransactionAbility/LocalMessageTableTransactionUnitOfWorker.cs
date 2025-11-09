using FreeSql.Various.Utilitys;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FreeSql.Various.Contexts;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    public class LocalMessageTableTransactionUnitOfWorker(
        ConcurrentDictionary<string, Func<string, Task<bool>>> tasks,
        FreeSqlSchedule schedule,
        LocalMessageTableDispatchConfig config,
        VariousTenantContext tenantContext)
    {
        private uint _reliableCount = 0;

        private readonly string _id = Guid.NewGuid().ToString();

        private Guid _fsqlIdentifier;

        private DataType _fsqlDataType;

        private string _content = string.Empty;

        private string _taskKey = string.Empty;

        private string _governing = string.Empty;

        private string _tenantMark = string.Empty;

        /// <summary>
        /// 借助事务持久化本地消息表
        /// </summary>
        /// <param name="tranFreeSql">事务对象</param>
        /// <param name="taskKey">任务Key</param>
        /// <param name="content">任务内容</param>
        /// <param name="governing">任务调度者</param>
        /// <param name="group">任务组</param>
        /// <param name="tenantMark"></param>
        /// <exception cref="Exception"></exception>
        public void Reliable(IFreeSql tranFreeSql, string taskKey, string content, string governing, string group,
            string tenantMark)
        {
            // 一个对象 防止绑定多次
            if (Interlocked.Add(ref _reliableCount, 1) > 1)
            {
                throw new Exception("LocalMessageTableTransactionUnitOfWorker 不可Reliable多次!");
            }

            _fsqlIdentifier = tranFreeSql.Ado.Identifier;

            _fsqlDataType = tranFreeSql.Ado.DataType;

            _content = content;

            _taskKey = taskKey;

            _governing = governing;

            _tenantMark = tenantMark;

            VariousMemoryCache.LocalMessageTableTaskDescribe.TryGetValue(taskKey, out var describe);

            //执行的时候同步一次本地消息表
            var syncResult = VariousMemoryCache.LazySyncLocalMessageTable.GetOrAdd(_fsqlIdentifier,
                new Lazy<bool>(() =>
                {
                    try
                    {
                        var key = schedule.IdleBus()
                            .GetKeys(elaborate =>
                            {
                                if (elaborate == null)
                                    return false;

                                return elaborate.FreeSql.Ado.Identifier == _fsqlIdentifier &&
                                       elaborate.FreeSql.Ado.DataType == _fsqlDataType;
                            })
                            .FirstOrDefault();

                        var ela = schedule.Get(key!);

                        if (VariousMemoryCache.IsSyncLocalMessageTable.TryGetValue(
                                ela.FreeSql.Ado.ConnectionString.GetHashCode(), out bool value) && value)
                        {
                            return true;
                        }
                        else
                        {
                            ela.FreeSql.CodeFirst.SyncStructure<LocalMessageDatabaseTableLogger>();
                            ela.FreeSql.CodeFirst.SyncStructure<LocalMessageGoverningDatabaseTable>();
                            ela.FreeSql.CodeFirst.SyncStructure<LocalMessageDatabaseTable>();
                            VariousMemoryCache.IsSyncLocalMessageTable.TryAdd(
                                ela.FreeSql.Ado.ConnectionString.GetHashCode(),
                                true);
                        }
                    }
                    catch (Exception e)
                    {
                        VariousConsole.Error<LocalMessageTableTransactionUnitOfWorker>(
                            $"同步本地消息表失败「{tranFreeSql.Ado.ConnectionString}」,「Exception」: {e}");

                        return false;
                    }

                    return true;
                }));

            _ = syncResult.Value;

            describe ??= "无描述";

            if (string.IsNullOrEmpty(governing))
            {
                //添加消息表数据
                tranFreeSql.Insert(new LocalMessageDatabaseTable
                {
                    Id = _id,
                    TaskKey = taskKey,
                    TaskDescribe = describe,
                    MessageTime = DateTime.Now,
                    Retries = 0,
                    MessageContent = content,
                }).ExecuteAffrows();

                tranFreeSql.Insert(new LocalMessageDatabaseTableLogger
                {
                    Id = _id,
                    TaskKey = taskKey,
                    TaskDescribe = describe,
                    MessageTime = DateTime.Now,
                    MessageContent = content,
                    Group = string.Empty,
                    Governing = "default",
                    GroupEnsureOrderliness = false,
                }).ExecuteAffrows();
            }
            else
            {
                var groupIsConfig = config.GoverningSchedules.TryGetValue(governing, out var groupDispatchSchedule);

                var groupEnsureOrderliness =
                    groupIsConfig && groupDispatchSchedule != null &&
                    groupDispatchSchedule.GroupEnsureOrderliness.Any(g => g.Key == group);

                //添加消息表数据
                tranFreeSql.Insert(new LocalMessageGoverningDatabaseTable
                {
                    Id = _id,
                    TaskKey = taskKey,
                    Governing = governing,
                    Group = group,
                    GroupEnsureOrderliness = groupEnsureOrderliness,
                    TaskDescribe = describe,
                    MessageTime = DateTime.Now,
                    Retries = 0,
                    MessageContent = content,
                }).ExecuteAffrows();

                tranFreeSql.Insert(new LocalMessageDatabaseTableLogger
                {
                    Id = _id,
                    TaskKey = taskKey,
                    TaskDescribe = describe,
                    MessageTime = DateTime.Now,
                    MessageContent = content,
                    Governing = governing,
                    Group = group,
                    GroupEnsureOrderliness = groupEnsureOrderliness,
                }).ExecuteAffrows();
            }
        }

        /// <summary>
        /// 立即执行任务
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> DoAsync()
        {
            var key = schedule.IdleBus()
                .GetKeys(elaborate =>
                {
                    if (elaborate == null)
                        return false;

                    return elaborate.FreeSql.Ado.Identifier == _fsqlIdentifier &&
                           elaborate.FreeSql.Ado.DataType == _fsqlDataType;
                })
                .FirstOrDefault();

            if (key == null)
            {
                throw new Exception($"[{_fsqlIdentifier}]未注册.");
            }

            var ela = schedule.Get(key);

            var execResult = await ScheduleDoAsync(_id, _taskKey, _content, _governing, ela.FreeSql, _tenantMark);

            return execResult;
        }

        internal async Task<bool> ScheduleDoAsync(string id, string taskKey, string content, string governing,
            IFreeSql db, string tenantMark)
        {
            var tryGetValue =
                tasks.TryGetValue(taskKey,
                    out var method);

            if (!tryGetValue)
            {
                VariousConsole.Error<LocalMessageTableTransactionUnitOfWorker>($"没有找到{taskKey}任务.");
                return false;
            }

            Exception? exception = null;

            var execResult = false;

            try
            {
                tenantContext.Set(tenantMark);
                execResult = await method!.Invoke(content);
                tenantContext.Clear();
            }
            catch (Exception e)
            {
                exception = e;
            }

            //补偿则删除本条消息
            if (execResult)
            {
                if (string.IsNullOrEmpty(governing))
                    await db.Delete<LocalMessageDatabaseTable>().Where(f => f.Id == id).ExecuteAffrowsAsync();
                else
                    await db.Delete<LocalMessageGoverningDatabaseTable>().Where(f => f.Id == id).ExecuteAffrowsAsync();
            }
            else
            {
                //记录失败原因
                if (exception == null) return execResult;

                if (string.IsNullOrEmpty(governing))
                {
                    await db.Update<LocalMessageDatabaseTable>()
                        .Set(f => f.ErrorMessage, exception.ToString())
                        .Where(f => f.Id == id)
                        .ExecuteAffrowsAsync();
                }
                else
                {
                    await db.Update<LocalMessageGoverningDatabaseTable>()
                        .Set(f => f.ErrorMessage, exception.ToString())
                        .Where(f => f.Id == id)
                        .ExecuteAffrowsAsync();
                }

                await db.Update<LocalMessageDatabaseTableLogger>()
                    .Set(f => f.ErrorMessage, exception.ToString())
                    .Where(f => f.Id == id)
                    .ExecuteAffrowsAsync();
            }

            return execResult;
        }
    }
}