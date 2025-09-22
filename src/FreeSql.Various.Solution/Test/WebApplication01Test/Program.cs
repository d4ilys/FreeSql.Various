using FreeSql.Various;
using FreeSql.Various.Dashboard;

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
                various.Dashboard.CustomExecutors.Add(new VariousDashboardCustomExecutor
                {
                    ExecutorId = "database_manager_codefirst_init",
                    ExecutorTitle = "ͬ��Settings���ݿ�����",
                    Executor = elements =>
                    {
                        Console.WriteLine("��Ҫ��ʼͬ����..");
                        elements.Notification("֪ͨ", "���ڽ���ͬ��..", VariousExecutorNotificationType.Info);
                        Thread.Sleep(3000);
                        elements.Notification("֪ͨ.", "ͬ�����..", VariousExecutorNotificationType.Success);
                        return Task.FromResult(true);
                    }
                });

                various.Dashboard.CustomExecutors.Add(new VariousDashboardCustomExecutor
                {
                    ExecutorId = "order_codefirst_init",
                    ExecutorTitle = "ͬ��Order���ݿ�����",
                    Executor = elements =>
                    {
                        foreach (var i in Enumerable.Range(0, 100))
                        {
                            Thread.Sleep(10);
                            elements.ShowLoading($"���ڴ��������� {i}%");
                        }

                        elements.HideLoading();
                        elements.Message("�����������", VariousExecutorNotificationType.Success, 2000);
                        return Task.FromResult(true);
                    }
                });
                options.DashboardPath = "VariousDashboard";
                options.Enable = true;
                options.FreeSqlSchedule = various.Schedule;
                options.VariousDashboard = various.Dashboard;
            });

            app.MapControllers();

            app.Run();
        }
    }
}