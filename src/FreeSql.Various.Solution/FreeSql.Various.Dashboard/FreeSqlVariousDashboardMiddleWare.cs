using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
            app.UseStaticFiles();
            app.Use(async (context, next) =>
            {
                var httpMethod = context.Request.Method;

                var path = context.Request.Path.Value;

                // If the RoutePrefix is requested (with or without trailing slash), redirect to index URL
                if (httpMethod == "GET" && path != null &&
                    Regex.IsMatch(path, $"^/?{Regex.Escape(optionsInternal.DashboardPath)}/?$"))
                {
                    // Use relative redirect to support proxy environments
                    var relativeRedirectPath = path.EndsWith("/")
                        ? "index.html"
                        : $"{path.Split('/').Last()}/index.html";

                    RespondWithRedirect(context.Response, relativeRedirectPath);
                    return;
                }

                if (httpMethod == "GET" && path != null &&
                    Regex.IsMatch(path, $"^/{Regex.Escape(optionsInternal.DashboardPath)}/?index.html$"))
                {
                    await RespondWithIndexHtmlAsync(context.Response);
                    return;
                }

                await next();
            });

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
                await executor?.Executor.Invoke(elements)!;

                context.Response.Body.Close();
            });

            return app;
        }

        private static void RespondWithRedirect(HttpResponse response, string location)
        {
            response.StatusCode = 301;
            response.Headers["Location"] = location;
        }

        private static async Task RespondWithIndexHtmlAsync(HttpResponse response)
        {
            response.StatusCode = 200;
            response.ContentType = "text/html;charset=utf-8";

            await using var stream = typeof(FreeSqlVariousDashboardMiddlewareExtension).GetTypeInfo().Assembly
                .GetManifestResourceStream("FreeSql.Various.Dashboard.index.html");

            // Inject arguments before writing to response
            if (stream != null)
            {
                var htmlBuilder = new StringBuilder(await new StreamReader(stream).ReadToEndAsync());

                await response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
            }
        }
    }
}