using Microsoft.Extensions.DependencyInjection;

namespace FreeSql.Various.Dashboard
{
    public class VariousDashboardCustomExecutor
    {
        /// <summary>
        /// 执行器Id
        /// </summary>
        public string ExecutorId { get; set; }

        /// <summary>
        /// 执行器标题
        /// </summary>
        public string ExecutorTitle { get; set; }

        /// <summary>
        /// 执行动作
        /// </summary>
        public Func<VariousDashboardCustomExecutorUiElements, Task<bool>>? ExecutorDelegate { get; set; }

        /// <summary>
        /// 注册执行器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">在Class具体实现</param>
        /// <param name="executorInstanceDelegate">Class实例 如果存在有参构造函数自定义实例化</param>
        public void RegisterExecutor<T>(
            Func<T, Func<VariousDashboardCustomExecutorUiElements, Task<bool>>> func,
            Func<T>? executorInstanceDelegate = null) where T : class
        {
            try
            {
                var obj = executorInstanceDelegate?.Invoke();
                //反射创建T
                var t = obj ?? Activator.CreateInstance(typeof(T));
                ExecutorDelegate = elements => func(t as T ?? throw new Exception("添加执行器错误"))(elements);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}