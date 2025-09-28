using FreeSql.Various;
using FreeSql.Various.Dashboard;
using FreeSql.Various.Dashboard.Models;
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

                various.Dashboard.RegisterCustomExecutor("ͬ������", executors =>
                {
                    executors.Add(executor =>
                    {
                        executor.ExecutorId = nameof(OrderExecutor.OrderCodeFirst);
                        executor.ExecutorTitle = "ͬ��Order���б�";
                        executor.RegisterExecutor<OrderExecutor>(e => e.OrderCodeFirst);
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = nameof(ProductExecutor.ProductCodeFirst);
                        executor.ExecutorTitle = "ͬ��Product���ݿ�����";
                        executor.RegisterExecutor<ProductExecutor>(e => e.ProductCodeFirst);
                    });
                });

                various.Dashboard.RegisterCustomExecutor("������������", executors =>
                {
                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_openurl";
                        executor.ExecutorTitle = "���Դ���ҳ";
                        executor.ExecutorDelegate = elements =>
                        {
                            elements.OpenUrl("http://baidu.com");
                            return Task.FromResult(true);
                        };
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_confirm";
                        executor.ExecutorTitle = "����ȷ�Ͽ�";
                        executor.ExecutorDelegate = elements =>
                        {
                            elements.Alert("��ʾ", "�����ȷ����ְ��", "");
                            return Task.FromResult(true);
                        };
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_modal_request";
                        executor.ExecutorTitle = "����ģ̬��";
                        executor.ExecutorDelegate = elements =>
                        {
                            var config = new ModalFormRequestConfig
                            {
                                Title = "����ģ̬��",
                                Router = "Home/TestModal",
                                Components =
                                [
                                    new()
                                    {
                                        Type = ModalFormComponentType.Text,
                                        Label = "���ݿ�����",
                                        Name = "databaseName",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "���������ݿ�����"
                                        }
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.TextArea,
                                        Label = "���ݿ�����",
                                        Name = "connectionString",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "���������ݿ�����"
                                        }
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.Select,
                                        Label = "���ݿ�����",
                                        Name = "databaseType",
                                        DefaultValue = "SqlServer",
                                        Options =
                                        [
                                            new { value = "SqlServer", label = "SqlServer���ݿ�" },
                                            new { value = "MySql", label = "MySql���ݿ�" },
                                        ]
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.Text,
                                        Label = "����ʱ��",
                                        Name = "createTime",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "�����봴��ʱ��"
                                        }
                                    }
                                ]
                            };
                            elements.ModalFromRequest(config);
                            return Task.FromResult(true);
                        };
                    });
                });
            });

            app.MapControllers();

            app.Run();
        }
    }
}