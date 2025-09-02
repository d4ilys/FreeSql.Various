using System.Collections.Concurrent;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    public class LocalMessageTableTransactionUnitOfWorker(
        ConcurrentDictionary<string, Func<string, Task<bool>>> tasks,
        FreeSqlSchedule schedule)
    {
        private uint _reliableCount = 0;

        private readonly string _id = Guid.NewGuid().ToString();

        private string _fsqlConnectionString = string.Empty;

        private DataType _fsqlDataType;

        private string _content = string.Empty;

        private string _taskKey = string.Empty;

        /// <summary>
        /// 借助事务持久化本地消息表
        /// </summary>
        /// <param name="tranFreeSql">事务对象</param>
        /// <param name="taskKey">任务Key</param>
        /// <param name="content">任务内容</param>
        /// <param name="describe">任务描述</param>
        /// <exception cref="Exception"></exception>
        public void Reliable(ref IFreeSql tranFreeSql, string taskKey, string content, string describe = "")
        {
            // 一个对象 防止绑定多次
            if (Interlocked.Add(ref _reliableCount, 1) > 1)
            {
                throw new Exception("LocalMessageTableTransactionUnitOfWorker 不可Reliable多次!");
            }

            _fsqlConnectionString = tranFreeSql.Ado.ConnectionString;

            _fsqlDataType = tranFreeSql.Ado.DataType;

            _content = content;

            _taskKey = taskKey;

            //添加消息表数据
            tranFreeSql.Insert(new FinalConsistencyMessage
            {
                Id = _id,
                TaskKey = taskKey,
                TaskDescribe = describe,
                MessageGroup = 0,
                MessageTime = DateTime.Now,
                Retries = 1,
                MessageContent = content
            }).ExecuteAffrows();
        }

        /// <summary>
        /// 立即执行任务
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> DoAsync()
        {
            var key = schedule.GetIdleBus()
                .GetKeys(db => db.Ado.ConnectionString == _fsqlConnectionString && db.Ado.DataType == _fsqlDataType)
                .FirstOrDefault();

            if (key == null)
            {
                throw new Exception($"[{_fsqlConnectionString}]未注册.");
            }

            var db = schedule.Get(key);

            var execResult = await ScheduleDoAsync(_taskKey, _content, db);

            return execResult;
        }

        internal async Task<bool> ScheduleDoAsync(string taskKey, string content, IFreeSql db)
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
                execResult = await method!.Invoke(content);
            }
            catch (Exception e)
            {
                exception = e;
            }

            //补偿则删除本条消息
            if (execResult)
            {
                await db.Delete<FinalConsistencyMessage>().Where(f => f.Id == _id).ExecuteAffrowsAsync();
            }
            else
            {
                //记录失败原因
                if (exception != null)
                {
                    await db.Update<FinalConsistencyMessage>()
                        .Set(f => f.ErrorMessage, exception.ToString())
                        .Where(f => f.Id == _id)
                        .ExecuteAffrowsAsync();
                }
            }

            return true;
        }
    }
}