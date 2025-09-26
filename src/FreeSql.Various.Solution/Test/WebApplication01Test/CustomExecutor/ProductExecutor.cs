using System.Text.Encodings.Web;
using System.Text.Json;
using FreeSql.Various.Dashboard;
using FreeSql.Various.Dashboard.Models;

namespace WebApplication01Test.CustomExecutor
{
    public class ProductExecutor
    {
        public Task<bool> ProductCodeFirst(VariousDashboardCustomExecutorUiElements elements)
        {
            var sql = """
                      CREATE TABLE IF NOT EXISTS `cccddd`.`Topic` (
                          `Id` INT(11) NOT NULL AUTO_INCREMENT,
                          `Clicks` INT(11) NOT NULL,
                          `Title` VARCHAR(255),
                          `CreateTime` DATETIME NOT NULL,
                          `fusho` SMALLINT(5) UNSIGNED NOT NULL,
                          PRIMARY KEY (`Id`)
                      ) Engine=InnoDB CHARACTER SET utf8;
                      """;
            var jsonBody = new
            {
                Ddl = sql,
                Database = "Topic"
            };
            var contentStyle = new Dictionary<string, string>()
            {
                ["fontSize"] = "12px",
            };
            elements.AfterConfirmRequest(new ConfirmRequestConfig
            {
                ConfirmDialogTitle = "同步确认",
                ConfirmDialogContent = sql,
                Router = "/Home/Index",
                JsonBody = JsonSerializer.Serialize(jsonBody, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }),
                ContentStyle = contentStyle,
                Headers = new Dictionary<string, string>() { ["Tenant"] = "lemi" },
                Payload = ""
            });
            return Task.FromResult(true);
            return Task.FromResult(true);
        }
    }
}