using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

            app.MapGet("/getExecutors", () =>
            {
                var executors = optionsInternal.VariousDashboard.CustomExecutors;

                var enumerable = executors
                    .Select(executor => new { id = executor.ExecutorId, title = executor.ExecutorTitle })
                    .ToList();
                return enumerable;
            });

            app.MapGet("/executor", async context =>
            {
                var response = context.Response;
                //响应头部添加text/event-stream
                response.Headers.Append("Content-Type", "text/event-stream");
                await response.WriteAsync($"event:handler\r\r");
                var id = context.Request.Query["id"];
                var executor = optionsInternal.VariousDashboard.CustomExecutors.FirstOrDefault(e => e.ExecutorId == id);

                var elements = new VariousDashboardCustomExecutorUiElements()
                {
                    SendMessageFunc = async message =>
                    {
                        await response.WriteAsync($"data:{message}\r\r");
                        await response.Body.FlushAsync();
                    }
                };
                await executor?.ExecutorDelegate.Invoke(elements)!;

                context.Response.Body.Close();
            });

            return app;
        }
    }
}