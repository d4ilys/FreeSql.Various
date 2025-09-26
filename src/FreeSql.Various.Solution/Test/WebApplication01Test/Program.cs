using FreeSql.Various;
using FreeSql.Various.Dashboard;
using WebApplication01Test.CustomExecutor;

namespace WebApplication01Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<FreeSqlVarious>();

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    corsPolicyBuilder =>
                    {
                        corsPolicyBuilder.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader()
                            .AllowCredentials();
                    });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseCors("CorsPolicy");

            app.UseVariousDashboard(options =>
            {
                var various = app.Services.GetRequiredService<FreeSqlVarious>();

                options.DashboardPath = "VariousDashboard";
                options.Enable = true;
                options.FreeSqlSchedule = various.Schedule;
                options.VariousDashboard = various.Dashboard;

                various.Dashboard.CustomExecutors.Add(executor =>
                {
                    executor.ExecutorId = nameof(SettingsExecutor.SettingsCodeFirst);
                    executor.ExecutorTitle = "同步Settings数据库管理表";
                    executor.RegisterExecutor(e => e.SettingsCodeFirst,
                        () => ActivatorUtilities.CreateInstance<SettingsExecutor>(app.Services));
                });

                various.Dashboard.CustomExecutors.Add(executor =>
                {
                    executor.ExecutorId = nameof(OrderExecutor.OrderCodeFirst);
                    executor.ExecutorTitle = "同步Order所有表";
                    executor.RegisterExecutor<OrderExecutor>(e => e.OrderCodeFirst);
                });


                various.Dashboard.CustomExecutors.Add(executor =>
                {
                    executor.ExecutorId = nameof(ProductExecutor.ProductCodeFirst);
                    executor.ExecutorTitle = "同步Product数据库管理表";
                    executor.RegisterExecutor<ProductExecutor>(e => e.ProductCodeFirst);
                });


                various.Dashboard.CustomExecutors.Add(executor =>
                {
                    executor.ExecutorId = "test_openurl";
                    executor.ExecutorTitle = "测试打开网页";
                    executor.ExecutorDelegate = elements =>
                    {
                        elements.OpenUrl("http://baidu.com");
                        return Task.FromResult(true);
                    };
                });

                various.Dashboard.CustomExecutors.Add(executor =>
                {
                    executor.ExecutorId = "test_confirm";
                    executor.ExecutorTitle = "测试确认框";
                    executor.ExecutorDelegate = elements =>
                    {
                        elements.Alert("提示", "你真的确认离职吗？", "");
                        return Task.FromResult(true);
                    };
                });
            });

            app.MapControllers();

            app.Run();
        }
    }
}