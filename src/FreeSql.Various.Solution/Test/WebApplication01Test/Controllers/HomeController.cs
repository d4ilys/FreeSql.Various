using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication01Test.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public record IndexParam(string Ddl, string Database);

        public IActionResult Index([FromBody] IndexParam param)
        {
            return Ok(new
                { message = "Hello World!", tenant = HttpContext.Request.Headers["Tenant"] });
        }

        public record TestModalParam(
            string DatabaseName,
            string ConnectionString,
            string DatabaseType,
            DateTime CreateTime);

        public string TestModal([FromBody] TestModalParam param)
        {
            return JsonSerializer.Serialize(param, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
}