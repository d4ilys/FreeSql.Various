using System.Text.Json;
using Demo01.Tables;
using Demo01.TestClass;
using FreeSql;
using FreeSql.Various;
using FreeSql.Various.SeniorTransactions;
using FreeSql.Various.SeniorTransactions.CrossDatabaseTransactionAbility;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;
using FreeSql.Various.Utilitys;
using System.Text.RegularExpressions;
using FreeSql.Various.Sharing;

namespace Demo01;

class Program
{
    private static readonly FreeSqlVarious Various = new FreeSqlVarious();

    static async Task Main(string[] args)
    {
        Initialize();
    }


    static async Task Initialize()
    {
        //注册基础设置库
        Various.Register(DbEnum.Settings, () => new FreeSqlBuilder()
            .UseMonitorCommand(command => VariousConsole.Info<Program>(command.CommandText))
            .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("settings")}")
            .Build());

        var settingDb = Various.Use(DbEnum.Settings);

        await DatabaseSettingsInitialize(settingDb);

        var tenants = await settingDb.Select<TenantManager>().ToListAsync();

        var sharingDatabases = await settingDb.Select<SharingDatabaseManager>().ToListAsync();

        var allDatabaseConnection = await settingDb.Select<AllDatabaseConnectionManager>().ToListAsync();

        foreach (var sharingDatabaseManager in sharingDatabases.ToLookup(m => m.DbEnumName))
        {
            var dbEnum = Enum.Parse<DbEnum>(sharingDatabaseManager.Key);
            var first = sharingDatabaseManager.FirstOrDefault();
            if (first == null) continue;

            var template = first.Config["DatabaseNamingTemplate"].ToString();
            if (template == null) continue;

            switch (first.SharingPattern)
            {
                case VariousSharingPatternEnum.TimeRange:
                    var timeRangeConfig = new TimeRangeShardingRegisterConfigure
                    {
                        DatabaseNamingTemplate = template
                    };

                    //配置租户的分库逻辑
                    foreach (var databaseManager in sharingDatabaseManager)
                    {
                        var tenantMark = tenants.First(t => t.Id == databaseManager.TenantId).Mark;
                        var period = databaseManager.Config["Period"].ToString();
                        var startTime = DateTime.Parse(databaseManager.Config["SharingStartTime"].ToString());
                        timeRangeConfig.TenantConfigure.Add(new TimeRangeShardingRegisterTenantConfigure
                        {
                            SharingStartTime = startTime,
                            Period = period,
                            TenantMark = tenantMark
                        });

                        //构建所有相关数据库
                        foreach (var connection in allDatabaseConnection.Where(a => a.DatabaseId == databaseManager.Id))
                        {
                            timeRangeConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                            {
                                Database = connection.DatabaseName,
                                BuildIFreeSqlDelegate = () =>
                                    new FreeSqlBuilder()
                                        .UseConnectionString(connection.DataType, connection.ConnectionString)
                                        .UseMonitorCommand(cmd =>
                                            VariousConsole.Info<Program>($"{connection.DatabaseName}:{cmd}"))
                                        .Build()
                            });
                        }
                    }

                    // 注册数据库
                    Various.SharingPatterns.TimeRange.Register(dbEnum, timeRangeConfig);
                    break;
                case VariousSharingPatternEnum.Hash:

                    var hashConfig = new HashShardingRegisterConfigure
                    {
                        DatabaseNamingTemplate = template
                    };
                    //配置租户的分库逻辑
                    foreach (var databaseManager in sharingDatabaseManager)
                    {
                        var tenantMark = tenants.First(t => t.Id == databaseManager.TenantId).Mark;
                        var size = databaseManager.Config["Size"].ToString();
                        hashConfig.TenantConfigure.Add(new HashShardingRegisterTenantConfigure
                        {
                            Size = Convert.ToInt32(size),
                            TenantMark = tenantMark
                        });

                        //构建所有相关数据库
                        foreach (var connection in allDatabaseConnection.Where(a => a.DatabaseId == databaseManager.Id))
                        {
                            hashConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                            {
                                Database = connection.DatabaseName,
                                BuildIFreeSqlDelegate = () =>
                                    new FreeSqlBuilder()
                                        .UseConnectionString(connection.DataType, connection.ConnectionString)
                                        .UseMonitorCommand(cmd =>
                                            VariousConsole.Info<Program>($"{connection.DatabaseName}:{cmd}"))
                                        .Build()
                            });
                        }
                    }

                    // 注册数据库
                    Various.SharingPatterns.Hash.Register(dbEnum, hashConfig);
                    break;
                case VariousSharingPatternEnum.Tenant:
                    var tenantConfig = new TenantSharingRegisterConfigure
                    {
                        DatabaseNamingTemplate = template
                    };
                    foreach (var databaseManager in sharingDatabaseManager)
                    {
                        //构建所有相关数据库
                        foreach (var connection in allDatabaseConnection.Where(a => a.DatabaseId == databaseManager.Id))
                        {
                            tenantConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                            {
                                Database = connection.DatabaseName,
                                BuildIFreeSqlDelegate = () =>
                                    new FreeSqlBuilder()
                                        .UseConnectionString(connection.DataType, connection.ConnectionString)
                                        .UseMonitorCommand(cmd =>
                                            VariousConsole.Info<Program>($"{connection.DatabaseName}:{cmd}"))
                                        .Build()
                            });
                        }
                    }

                    Various.SharingPatterns.Tenant.Register(dbEnum, tenantConfig);
                    break;
            }
        }


        Various.SharingPatterns.Tenant.Register(DbEnum.Basics, new TenantSharingRegisterConfigure
        {
            DatabaseNamingTemplate = "basics_{tenant}",
            FreeSqlRegisterItems =
            {
                new("basics_lemi", () => new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("basics_lemi")}")
                    .Build()),

                new("basics_whd", () => new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("basics_whd")}")
                    .Build()),
            }
        });


        //  Various.SharingPatterns.Tenant.Use(DbEnum.Basics);
    }

    static string GenerateDbPath(string dbName)
    {
        return Path.Combine("Databases", $"{dbName}.db");
    }

    /// <summary>
    /// 跨数据库数据库表格查询
    /// </summary>
    /// <returns></returns>
    static async Task TableQueryAsync()
    {
        var startTime = DateTime.Parse("2023-01-01");
        var endTime = DateTime.Parse("2025-01-02");

        //表格查询 
        var queryResults = await Various.SharingPatterns.TimeRange.TableCrossDatabaseQueryAsync(DbEnum.Order,
            startTime,
            endTime,
            QueryOrdersByDatabase,
            QueryOrdersByElasticSearch
        );

        //如果不跨库 则查询MySql
        async Task<TableCrossDbQueryOutcome<Orders>> QueryOrdersByDatabase(IFreeSql db)
        {
            var result = await db.Select<Orders>()
                .Count(out var count)
                .Where(a => a.CreateTime >= startTime && a.CreateTime < endTime)
                .ToListAsync();

            return new(data: result, total: count);
        }


        // 如果跨库 则查询ElasticSearch
        async Task<TableCrossDbQueryOutcome<Orders>> QueryOrdersByElasticSearch()
        {
            long count = 0;
            // 从 ElasticSearch 中查询数据
            await Task.Delay(1);
            return new([], count);
        }
    }

    /// <summary>
    /// 跨数据库操作
    /// </summary>
    /// <returns></returns>
    static async Task TestQuery()
    {
        // 跨库操作
        var operationResults = await Various.SharingPatterns.TimeRange.CrossDatabaseOperationAsync(DbEnum.Basics,
            DateTime.Parse("2023-01-01"),
            DateTime.Parse("2025-01-02"),
            async (db) => await db.Select<object>().ToListAsync());


        var queryResults = await Various.SharingPatterns.TimeRange.CrossDatabaseQueryAsync(DbEnum.Basics,
            DateTime.Parse("2023-01-01"),
            DateTime.Parse("2025-01-02"), async db => await db.Select<CrossDatabaseOperationOutcome>().ToListAsync());
    }

    static async Task CrossDatabaseTransactionTestAsync()
    {
        var transactions = Various.Transactions;

        // 正常数据库 不分库 无多租户
        var basics = Various.UseElaborate(DbEnum.Basics);
        // Hash 分片分库
        var order = Various.SharingPatterns.Hash.UseElaborate(DbEnum.Order, "100001");
        // 时间范围分库
        var product = Various.SharingPatterns.TimeRange.UseElaborate(DbEnum.Product, DateTime.Today);

        // 三个不同数据库的事务组合
        using var achieve =
            transactions.CrossDatabaseTransaction.Create("商品扣减,订单创建事务", basics, order, product);
        try
        {
            achieve.Begin();

            await achieve.Orms.Orm1.Delete<object>().Where(a => true).ExecuteAffrowsAsync();

            await achieve.Orms.Orm2.Delete<object>().Where(a => true).ExecuteAffrowsAsync();

            await achieve.Orms.Orm3.Delete<object>().Where(a => true).ExecuteAffrowsAsync();

            achieve.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine("商品扣减,订单创建事务 回滚");
            achieve.Rollback();
        }
    }

    static async Task LocalMessageTableTransactionTestAsync()
    {
        var localMessageTableTransaction = Various.Transactions.LocalMessageTableTransaction;

        //注册需要调度的数据库
        localMessageTableTransaction.RegisterScheduleDatabase(Various.Use(DbEnum.Basics));

        //注册任务
        localMessageTableTransaction.RegisterTaskExecutor("UserLoginCodeMessage", content =>
        {
            int timeout = 0;

            //根据Content 发送消息
            return Task.FromResult(true);
        });

        //启动调度器
        localMessageTableTransaction.StartScheduler(TimeSpan.FromMinutes(3));

        var localMessageTableUnitOfWorker = localMessageTableTransaction.CreateUnitOfWorker();

        using var repositoryUnitOfWork = Various.Use(DbEnum.Basics).CreateUnitOfWork();

        var orm = repositoryUnitOfWork.Orm;

        await orm.Delete<object>().Where(s => true).ExecuteAffrowsAsync();

        //创建本地消息表事务工作单元
        localMessageTableUnitOfWorker.Reliable(ref orm, "UserLoginCodeMessage", "342342", "用户发送短信后");

        _ = localMessageTableUnitOfWorker.DoAsync();
    }

    static async Task DatabaseSettingsInitialize(IFreeSql settings)
    {
        settings.CodeFirst.SyncStructure<SharingDatabaseManager>();

        settings.CodeFirst.SyncStructure<TenantManager>();

        await settings.Insert(new List<TenantManager>
        {
            new()
            {
                Id = 1,
                Name = "淘宝",
                Mark = "taobao",
            },
            new()
            {
                Id = 2,
                Name = "京东",
                Mark = "jd"
            },
        }).ExecuteAffrowsAsync();

        await settings.Insert(new List<SharingDatabaseManager>
            {
                new()
                {
                    Id = 1,
                    TenantId = 1,
                    DbEnumName = "Basics",
                    SharingPattern = VariousSharingPatternEnum.Tenant,
                    Config = new Dictionary<string, object>()
                    {
                        ["DatabaseNamingTemplate"] = "basics_{tenant}"
                    }
                },
                new()
                {
                    Id = 2,
                    TenantId = 1,
                    DbEnumName = "Order",
                    SharingPattern = VariousSharingPatternEnum.TimeRange,
                    Config = new Dictionary<string, object>
                    {
                        ["DatabaseNamingTemplate"] = "order_{tenant}_{range}",
                        ["SharingStartTime"] = "2024-01-01",
                        ["Period"] = "1 Year"
                    },
                },
                new()
                {
                    Id = 3,
                    TenantId = 1,
                    DbEnumName = "Product",
                    SharingPattern = VariousSharingPatternEnum.Hash,
                    Config = new Dictionary<string, object>
                    {
                        ["DatabaseNamingTemplate"] = "product_{tenant}_{slice}",
                        ["Size"] = 2
                    }
                },
                new()
                {
                    Id = 4,
                    TenantId = 2,
                    DbEnumName = "Basics",
                    SharingPattern = VariousSharingPatternEnum.Tenant,
                    Config = new Dictionary<string, object>
                    {
                        ["DatabaseNamingTemplate"] = "basics_{tenant}"
                    }
                },
                new()
                {
                    Id = 5,
                    TenantId = 2,
                    DbEnumName = "Order",
                    SharingPattern = VariousSharingPatternEnum.TimeRange,
                    Config = new Dictionary<string, object>
                    {
                        ["DatabaseNamingTemplate"] = "order_{tenant}_{range}",
                        ["SharingStartTime"] = "2020-01-01",
                        ["Period"] = "2 Year"
                    },
                },
                new()
                {
                    Id = 6,
                    TenantId = 2,
                    DbEnumName = "Product",
                    SharingPattern = VariousSharingPatternEnum.Hash,
                    Config = new Dictionary<string, object>
                    {
                        ["DatabaseNamingTemplate"] = "product_{tenant}_{slice}",
                        ["Size"] = 3
                    }
                },
            }
        ).ExecuteAffrowsAsync();

        await settings.Insert(new List<AllDatabaseConnectionManager>
            {
                new()
                {
                    DatabaseId = 1,
                    DatabaseName = "basics_taobao",
                    ConnectionString = $"Data Source={GenerateDbPath("basics_taobao")}",
                },
                new()
                {
                    DatabaseId = 2,
                    DatabaseName = "order_taobao_2024",
                    ConnectionString = $"Data Source={GenerateDbPath("order_taobao_2024")}",
                },
                new()
                {
                    DatabaseId = 2,
                    DatabaseName = "order_taobao_2025",
                    ConnectionString = $"Data Source={GenerateDbPath("order_taobao_2025")}",
                },
                new()
                {
                    DatabaseId = 3,
                    DatabaseName = "product_taobao_1",
                    ConnectionString = $"Data Source={GenerateDbPath("product_taobao_1")}",
                },
                new()
                {
                    DatabaseId = 3,
                    DatabaseName = "product_taobao_2",
                    ConnectionString = $"Data Source={GenerateDbPath("product_taobao_2")}",
                },
                new()
                {
                    DatabaseId = 4,
                    DatabaseName = "basics_jd",
                    ConnectionString = $"Data Source={GenerateDbPath("basics_jd")}",
                },
                new()
                {
                    DatabaseId = 5,
                    DatabaseName = "order_jd_2024",
                    ConnectionString = $"Data Source={GenerateDbPath("order_jd_2024")}",
                },
                new()
                {
                    DatabaseId = 5,
                    DatabaseName = "order_jd_2025",
                    ConnectionString = $"Data Source={GenerateDbPath("order_jd_2025")}",
                },
                new()
                {
                    DatabaseId = 6,
                    DatabaseName = "product_jd_1",
                    ConnectionString = $"Data Source={GenerateDbPath("product_jd_1")}",
                },
                new()
                {
                    DatabaseId = 6,
                    DatabaseName = "product_jd_2",
                    ConnectionString = $"Data Source={GenerateDbPath("product_jd_2")}",
                },
            }
        ).ExecuteAffrowsAsync();
    }
}