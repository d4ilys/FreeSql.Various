using System.Collections.Concurrent;
using System.Security.Cryptography;
using Demo01.TestClass;
using FreeSql;
using FreeSql.Various;
using FreeSql.Various.Contexts;
using FreeSql.Various.Sharing;
using FreeSql.Various.Utilitys;
using static Demo01.FreeSqlVariousInstance;


namespace Demo01
{
    public class DatabaseInitialize
    {
        public static async Task InitializeAsync()
        {
            //注册基础设置库
            Various.Register(DbEnum.Settings, () => new FreeSqlBuilder()
                .UseMonitorCommand(command => VariousConsole.Info<Program>(command.CommandText))
                .UseNoneCommandParameter(true)
                .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("settings")}")
                .Build());

            var settingDb = Various.Use(DbEnum.Settings);

            settingDb.UseJsonMap();

            await DatabaseSettingsInitialize(settingDb);

            var tenants = await settingDb.Select<TenantManager>().ToListAsync();

            var sharingDatabases = await settingDb.Select<SharingDatabaseManager>().ToListAsync();

            var allDatabaseConnection = await settingDb.Select<AllDatabaseConnectionManager>().ToListAsync();

            var lookup = sharingDatabases.ToLookup(m => m.DbEnumName);

            foreach (var sharingDatabaseManager in lookup)
            {
                var dbEnum = Enum.Parse<DbEnum>(sharingDatabaseManager.Key);
                var first = sharingDatabaseManager.FirstOrDefault();
                if (first == null) continue;

                var template = first.Config["DatabaseNamingTemplate"].ToString();
                if (template == null) continue;

                switch (first.SharingPattern)
                {
                    case VariousSharingPatternEnum.TimeRange:

                        SharingPatternTimeRangeRegisterInternal(sharingDatabaseManager, ref tenants,
                            ref allDatabaseConnection, ref template, ref dbEnum);
                        break;
                    case VariousSharingPatternEnum.Hash:

                        SharingPatternHashRegisterInternal(sharingDatabaseManager, ref tenants,
                            ref allDatabaseConnection,
                            ref template, ref dbEnum);
                        break;
                    case VariousSharingPatternEnum.Tenant:

                        SharingPatternTenantRegisterInternal(sharingDatabaseManager, ref tenants,
                            ref allDatabaseConnection,
                            ref template, ref dbEnum);
                        break;
                }
            }

            // await DatabaseBusinessInitialize();
        }


        // TimeRange 相关数据库注册
        static void SharingPatternTimeRangeRegisterInternal(
            IGrouping<string, SharingDatabaseManager> sharingDatabaseManager,
            ref List<TenantManager> tenants,
            ref List<AllDatabaseConnectionManager> allDatabaseConnection,
            ref string template,
            ref DbEnum dbEnum)
        {
            var timeRangeConfig = new TimeRangeShardingRegisterConfigure
            {
                DatabaseNamingTemplate = template
            };

            //配置租户的分库逻辑
            foreach (var databaseManager in sharingDatabaseManager)
            {
                var tenantMark = tenants.First(t => t.Id == databaseManager.TenantId).Mark;
                var period = databaseManager.Config["Period"].ToString();
                var startTime = DateTime.Parse(databaseManager.Config["SharingStartTime"].ToString()!);
                timeRangeConfig.TenantConfigure.Add(new TimeRangeShardingRegisterTenantConfigure
                {
                    SharingStartTime = startTime,
                    Period = period,
                    TenantMark = tenantMark
                });

                //构建所有相关数据库
                foreach (var connection in allDatabaseConnection.Where(a => a.SharingDatabaseId == databaseManager.Id))
                {
                    timeRangeConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                    {
                        Database = connection.DatabaseName,
                        BuildIFreeSqlDelegate = () =>
                            new FreeSqlBuilder()
                                .UseConnectionString(connection.DataType, connection.ConnectionString)
                                .UseMonitorCommand(cmd =>
                                    VariousConsole.Info<Program>($"{connection.DatabaseName} - {cmd.CommandText}"))
                                .Build()
                    });
                }
            }

            // 注册数据库
            Various.SharingPatterns.TimeRange.Register(dbEnum, timeRangeConfig);
        }

        /// Hash 相关数据库注册
        static void SharingPatternHashRegisterInternal(
            IGrouping<string, SharingDatabaseManager> sharingDatabaseManager,
            ref List<TenantManager> tenants,
            ref List<AllDatabaseConnectionManager> allDatabaseConnection,
            ref string template,
            ref DbEnum dbEnum)
        {
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
                foreach (var connection in allDatabaseConnection.Where(a => a.SharingDatabaseId == databaseManager.Id))
                {
                    hashConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                    {
                        Database = connection.DatabaseName,
                        BuildIFreeSqlDelegate = () =>
                            new FreeSqlBuilder()
                                .UseConnectionString(connection.DataType, connection.ConnectionString)
                                .UseMonitorCommand(cmd =>
                                    VariousConsole.Info<Program>($"{connection.DatabaseName} - {cmd.CommandText}"))
                                .Build()
                    });
                }
            }

            // 注册数据库
            Various.SharingPatterns.Hash.Register(dbEnum, hashConfig);
        }

        static void SharingPatternTenantRegisterInternal(
            IGrouping<string, SharingDatabaseManager> sharingDatabaseManager,
            ref List<TenantManager> tenants,
            ref List<AllDatabaseConnectionManager> allDatabaseConnection,
            ref string template,
            ref DbEnum dbEnum)
        {
            var tenantConfig = new TenantSharingRegisterConfigure
            {
                DatabaseNamingTemplate = template
            };
            foreach (var databaseManager in sharingDatabaseManager)
            {
                //构建所有相关数据库
                foreach (var connection in allDatabaseConnection.Where(a =>
                             a.SharingDatabaseId == databaseManager.Id))
                {
                    tenantConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
                    {
                        Database = connection.DatabaseName,
                        BuildIFreeSqlDelegate = () =>
                            new FreeSqlBuilder()
                                .UseConnectionString(connection.DataType, connection.ConnectionString)
                                .UseMonitorCommand(cmd =>
                                    VariousConsole.Info<Program>($"{connection.DatabaseName} - {cmd.CommandText}"))
                                .Build()
                    });
                }
            }

            Various.SharingPatterns.Tenant.Register(dbEnum, tenantConfig);
        }

        //Tenant相关
        static async Task DatabaseSettingsInitialize(IFreeSql settings)
        {
            settings.CodeFirst.SyncStructure<SharingDatabaseManager>();

            settings.CodeFirst.SyncStructure<AllDatabaseConnectionManager>();

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
                        SharingDatabaseId = 1,
                        DatabaseName = "basics_taobao",
                        ConnectionString = $"Data Source={GenerateDbPath("basics_taobao")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 2,
                        DatabaseName = "order_taobao_2024",
                        ConnectionString = $"Data Source={GenerateDbPath("order_taobao_2024")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 2,
                        DatabaseName = "order_taobao_2025",
                        ConnectionString = $"Data Source={GenerateDbPath("order_taobao_2025")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 3,
                        DatabaseName = "product_taobao_1",
                        ConnectionString = $"Data Source={GenerateDbPath("product_taobao_1")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 3,
                        DatabaseName = "product_taobao_2",
                        ConnectionString = $"Data Source={GenerateDbPath("product_taobao_2")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 4,
                        DatabaseName = "basics_jd",
                        ConnectionString = $"Data Source={GenerateDbPath("basics_jd")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 5,
                        DatabaseName = "order_jd_2024",
                        ConnectionString = $"Data Source={GenerateDbPath("order_jd_2024")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 5,
                        DatabaseName = "order_jd_2025",
                        ConnectionString = $"Data Source={GenerateDbPath("order_jd_2025")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 6,
                        DatabaseName = "product_jd_1",
                        ConnectionString = $"Data Source={GenerateDbPath("product_jd_1")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 6,
                        DatabaseName = "product_jd_2",
                        ConnectionString = $"Data Source={GenerateDbPath("product_jd_2")}",
                        DataType = DataType.Sqlite
                    },
                    new()
                    {
                        SharingDatabaseId = 6,
                        DatabaseName = "product_jd_3",
                        ConnectionString = $"Data Source={GenerateDbPath("product_jd_3")}",
                        DataType = DataType.Sqlite
                    },
                }
            ).ExecuteAffrowsAsync();
        }

        static async Task DatabaseBusinessInitialize()
        {
            async Task ProductInitialize()
            {
                ConcurrentDictionary<IFreeSql, List<Product>> products = new();
                string tenant = Various.TenantContext.GetCurrent();
                foreach (var i in Enumerable.Range(1, 10))
                {
                    var product = new Product
                    {
                        Id = i,
                        Name = $"Product-{tenant}{i}",
                        Price = i * 10
                    };

                    var fsql = Various.SharingPatterns.Hash.Use(DbEnum.Product, i.ToString());

                    var list = products.GetOrAdd(fsql, _ => []);

                    list.Add(product);
                }

                // 批量插入
                foreach (KeyValuePair<IFreeSql, List<Product>> keyValuePair in products)
                {
                    keyValuePair.Key.CodeFirst.SyncStructure<Product>();
                    await keyValuePair.Key.Insert(keyValuePair.Value).ExecuteAffrowsAsync();
                }
            }

            async Task OrderInitialize()
            {
                ConcurrentDictionary<IFreeSql, List<Order>> orders = new();
                string tenant = Various.TenantContext.GetCurrent();
                foreach (var i in Enumerable.Range(1, 10))
                {
                    var order = new Order
                    {
                        Id = i,
                        UserId = $"user-{tenant}-{i}",
                        ProductId = i,
                        Price = i * 10,
                        Status = "Completed",
                        OrderTime = DateTime.Now
                    };

                    var fsql = Various.SharingPatterns.TimeRange.Use(DbEnum.Order, order.OrderTime);

                    var list = orders.GetOrAdd(fsql, _ => []);

                    list.Add(order);
                }


                // 批量插入
                foreach (KeyValuePair<IFreeSql, List<Order>> keyValuePair in orders)
                {
                    keyValuePair.Key.CodeFirst.SyncStructure<Order>();
                    await keyValuePair.Key.Insert(keyValuePair.Value).ExecuteAffrowsAsync();
                }
            }

            await Task.WhenAll(
                ProductInitialize(),
                OrderInitialize()
            );
        }

        static string GenerateDbPath(string dbName)
        {
            string combine = Path.Combine("Databases", $"{dbName}.db");

            if (!Directory.Exists("Databases"))
                Directory.CreateDirectory("Databases");

            return combine;
        }
    }
}