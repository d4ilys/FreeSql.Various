using FreeSql.Various.Dashboard;

namespace WebApplication01Test.CustomExecutor
{
    public class SettingsExecutor(IConfiguration configuration)
    {
        public Task<bool> SettingsCodeFirst(VariousDashboardCustomExecutorUiElements elements)
        {
            var str = configuration["AllowedHosts"];
            foreach (var i in Enumerable.Range(0, 100))
            {
                Thread.Sleep(10);
                elements.ShowLoading($"正在处理数据中{str}{i}%");
            }

            elements.HideLoading();
            elements.Message("处理数据完成", VariousExecutorNotificationType.Success, 2000);
            return Task.FromResult(true);
        }
    }
}