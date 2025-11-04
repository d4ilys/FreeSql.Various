using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeSql.Various.Dashboard
{
    internal class FreeSqlVariousDashboardMiddleware
    {
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private readonly FreeSqlVariousDashboardOptions _options;

        public FreeSqlVariousDashboardMiddleware(RequestDelegate next,
            IWebHostEnvironment hostingEnv,
            ILoggerFactory loggerFactory,
            FreeSqlVariousDashboardOptions options)
        {
            _options = options;
            if (_options.DashboardPath.StartsWith("/"))
            {
                _options.DashboardPath = _options.DashboardPath.Substring(1);
            }
            _staticFileMiddleware = CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);
        }


        public async Task Invoke(HttpContext context)
        {
            var httpMethod = context.Request.Method;

            var path = context.Request.Path.Value;

            // If the RoutePrefix is requested (with or without trailing slash), redirect to index URL
            if (httpMethod == "GET" && path != null &&
                Regex.IsMatch(path, $"^/?{Regex.Escape(_options.DashboardPath)}/?$"))
            {
                
                // Use relative redirect to support proxy environments
                var relativeRedirectPath = path.EndsWith("/")
                    ? "index.html"
                    : $"{path.Split('/').Last()}/index.html";

                RespondWithRedirect(context.Response, relativeRedirectPath);
                return;
            }

            if (httpMethod == "GET" && path != null &&
                Regex.IsMatch(path, $"^/{Regex.Escape(_options.DashboardPath)}/?index.html$"))
            {
                await RespondWithIndexHtmlAsync(context.Response);
                return;
            }

            await _staticFileMiddleware.Invoke(context);
        }

        StaticFileMiddleware CreateStaticFileMiddleware(
            RequestDelegate next,
            IWebHostEnvironment hostingEnv,
            ILoggerFactory loggerFactory,
            FreeSqlVariousDashboardOptions options)
        {
            string embeddedFileNamespace = "FreeSql.Various.Dashboard";
            var staticFileOptions = new StaticFileOptions
            {
                RequestPath = string.IsNullOrEmpty(options.DashboardPath) ? string.Empty : $"/{options.DashboardPath}",
                FileProvider = new EmbeddedFileProvider(
                    typeof(FreeSqlVariousDashboardMiddleware).GetTypeInfo().Assembly,
                    embeddedFileNamespace),
            };

            return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }

        private void RespondWithRedirect(HttpResponse response, string location)
        {
            response.StatusCode = 301;
            response.Headers["Location"] = location;
        }

        private async Task RespondWithIndexHtmlAsync(HttpResponse response)
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