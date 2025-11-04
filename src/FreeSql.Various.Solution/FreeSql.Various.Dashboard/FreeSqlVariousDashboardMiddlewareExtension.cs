using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FreeSql.Various.Dashboard
{
    public static class FreeSqlVariousDashboardMiddlewareExtension
    {
        public static WebApplication UseVariousDashboard(this WebApplication app,
            Action<FreeSqlVariousDashboardOptions> options)
        {
            var optionsInternal = new FreeSqlVariousDashboardOptions();

            options(optionsInternal);

            app.UseMiddleware<FreeSqlVariousDashboardMiddleware>(optionsInternal);

            app.MapGet($"{optionsInternal.DashboardPath}/getExecutors", () =>
            {
                var executors = optionsInternal.VariousDashboard.CustomExecutors;

                var res = executors.Select(pair => new
                {
                    group = pair.Key,
                    executors = pair.Value.Select(executor => new
                    {
                        id = executor.ExecutorId,
                        title = executor.ExecutorTitle,
                        group = pair.Key
                    })
                }).ToList();

                return res;
            });

            app.MapGet($"{optionsInternal.DashboardPath}/executor", async context =>
            {
                var response = context.Response;
                //响应头部添加text/event-stream
                response.Headers.Append("Content-Type", "text/event-stream");
                await response.WriteAsync($"event:handler\r\r");
                var id = context.Request.Query["id"];
                var group = context.Request.Query["group"].ToString();
                var executor = optionsInternal.VariousDashboard.CustomExecutors[group]
                    .FirstOrDefault(e => e.ExecutorId == id);

                var elements = new VariousDashboardCustomExecutorUiElements()
                {
                    SendMessageFunc = async message =>
                    {
                        await response.WriteAsync($"data:{message}\r\r");
                        await response.Body.FlushAsync();
                    }
                };

                await executor?.ExecutorDelegate?.Invoke(elements)!;

                context.Response.Body.Close();
            });

            return app;
        }
    }
}