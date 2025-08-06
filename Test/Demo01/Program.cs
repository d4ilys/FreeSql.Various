using System.Text.RegularExpressions;
using FreeSql;
using FreeSql.Various;
using FreeSql.Various.Utilitys;

namespace Demo01;

class Program
{
    static void Main(string[] args)
    {
        var various = new FreeSqlVarious();
        various.SharingPatterns.Tenant.Register(DbEnum.Basics, new TenantSharingRegisterConfigure
        {
            DatabaseNamingTemplate = "basics_{tenant}",
            FreeSqlRegisterItems =
            {
                new("basics_lemi", () => new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, $"Data Source=:memory:")
                    .Build()),

                new("basics_whd", () => new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, $"Data Source=:memory:")
                    .Build()),
            }
        });

        various.SharingPatterns.Tenant.Use(DbEnum.Basics);
    }
}