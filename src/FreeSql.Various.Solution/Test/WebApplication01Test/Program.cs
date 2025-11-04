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

                options.DashboardPath = "erp/various";
                options.Enable = true;
                options.FreeSqlSchedule = various.Schedule;
                options.VariousDashboard = various.Dashboard;

                various.Dashboard.RegisterCustomExecutor("同步表功能", executors =>
                {
                    executors.Add(executor =>
                    {
                        executor.ExecutorId = nameof(OrderExecutor.OrderCodeFirst);
                        executor.ExecutorTitle = "同步Order所有表";
                        executor.RegisterExecutor<OrderExecutor>(e => e.OrderCodeFirst);
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = nameof(ProductExecutor.ProductCodeFirst);
                        executor.ExecutorTitle = "同步Product数据库管理表";
                        executor.RegisterExecutor<ProductExecutor>(e => e.ProductCodeFirst);
                    });
                });

                various.Dashboard.RegisterCustomExecutor("测试其他功能", executors =>
                {
                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_openurl";
                        executor.ExecutorTitle = "测试打开网页";
                        executor.ExecutorDelegate = elements =>
                        {
                            elements.OpenUrl("http://baidu.com");
                            return Task.FromResult(true);
                        };
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_confirm";
                        executor.ExecutorTitle = "测试确认框";
                        executor.ExecutorDelegate = elements =>
                        {
                            elements.Alert("提示", "你真的确认离职吗？", "");
                            return Task.FromResult(true);
                        };
                    });

                    executors.Add(executor =>
                    {
                        executor.ExecutorId = "test_modal_request";
                        executor.ExecutorTitle = "测试模态框";
                        executor.ExecutorDelegate = elements =>
                        {
                            var config = new ModalFormRequestConfig
                            {
                                Title = "测试模态框",
                                Router = "Home/TestModal",
                                Components =
                                [
                                    new()
                                    {
                                        Type = ModalFormComponentType.Text,
                                        Label = "数据库名称",
                                        Name = "databaseName",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "请输入数据库名称"
                                        }
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.TextArea,
                                        Label = "数据库连接",
                                        Name = "connectionString",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "请输入数据库连接"
                                        }
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.Select,
                                        Label = "数据库类型",
                                        Name = "databaseType",
                                        DefaultValue = "SqlServer",
                                        Options =
                                        [
                                            new { value = "SqlServer", label = "SqlServer数据库" },
                                            new { value = "MySql", label = "MySql数据库" },
                                        ]
                                    },
                                    new()
                                    {
                                        Type = ModalFormComponentType.Text,
                                        Label = "创建时间",
                                        Name = "createTime",
                                        Rules = new FormRules
                                        {
                                            Required = true,
                                            Message = "请输入创建时间"
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