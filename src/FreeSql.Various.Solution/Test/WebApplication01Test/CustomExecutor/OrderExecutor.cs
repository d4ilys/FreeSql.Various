using FreeSql.Various.Dashboard;

namespace WebApplication01Test.CustomExecutor
{
    public class OrderExecutor
    {
        public Task<bool> OrderCodeFirst(VariousDashboardCustomExecutorUiElements elements)
        {
            foreach (var i in Enumerable.Range(0, 100))
            {
                Thread.Sleep(10);
                elements.ShowLoading($"正在处理数据中 {i}%");
            }

            elements.HideLoading();
            elements.Message("处理数据完成", VariousExecutorNotificationType.Success, 2000);
            return Task.FromResult(true);
        }
    }
}