using Demo01.Tables;
using Demo01.TestClass;
using FreeSql;
using FreeSql.Various;
using FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility;
using FreeSql.Various.Sharing;
using static Demo01.FreeSqlVariousInstance;

namespace Demo01;

class Program
{
    static async Task Main(string[] args)
    {
        Various.TenantContext.Set("taobao");
        await DatabaseInitialize.InitializeAsync();
        LocalMessageTableTransactionTest();
        while (true)
        {
            Console.ReadKey();
            await LocalMessageTableTransactionNormalTestAsync();
        }
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

    #region 本地消息表事务测试

    static void LocalMessageTableTransactionTest()
    {
        var localMessageTableTransaction = Various.SeniorTransactions.LocalMessageTableTransaction;

        var productDbs = Various.SharingPatterns.Hash.UseElaborateAll(DbEnum.Product);

        var orderDbs = Various.SharingPatterns.TimeRange.UseElaborateAll(DbEnum.Order);

        var dispatchDbs = productDbs.ToList();

        dispatchDbs.AddRange(orderDbs);

        //注册需要调度的数据库
        localMessageTableTransaction.RegisterDispatchDatabase(dispatchDbs.ToArray());

        //注册任务
        localMessageTableTransaction.RegisterTaskExecutor("ShippingNoticeERP", "通知ERP系统",
            content =>
            {
                var random = new Random().Next(0, 3);
                if (random == 3)
                {
                    throw new Exception($"当前随机数为{random},测试引发异常");
                }

                return Task.FromResult(random == 1);
            });

        //注册任务
        localMessageTableTransaction.RegisterTaskExecutor("ShippingNoticeWareHouse", "通知仓库系统",
            content =>
            {
                var random = new Random().Next(0, 3);
                if (random == 3)
                {
                    throw new Exception($"当前随机数为{random},测试引发异常");
                }

                return Task.FromResult(random == 1);
            });

        localMessageTableTransaction.RegisterTaskExecutor("OrderDelivery", "订单发货通知用户", content =>
        {
            var random = new Random().Next(0, 3);
            if (random == 3)
            {
                throw new Exception($"当前随机数为{random},测试引发异常");
            }

            return Task.FromResult(random == 1);
        });

        localMessageTableTransaction.ConfigDispatch(config =>
        {
            config.MainSchedule.Period = TimeSpan.FromMinutes(1);
            config.MainSchedule.DueTime = TimeSpan.FromSeconds(1);
            config.MainSchedule.MaxRetries = 20;

            //自定义任务组
            config.GoverningSchedules.Add("OrderNotice", new LocalMessageTableGoverningDispatchSchedule
            {
                GroupEnsureOrderliness = new Dictionary<string, bool>()
                {
                    ["Business1"] = true,
                    ["Business2"] = true,
                },
                Schedule = new LocalMessageTableDispatchSchedule
                {
                    Period = TimeSpan.FromSeconds(10),
                    DueTime = TimeSpan.FromSeconds(1),
                    MaxRetries = 100
                }
            });
        });

        localMessageTableTransaction.SyncAllDatabaseLocalMessageTable();

        //启动调度器
        localMessageTableTransaction.DispatchRunning();
    }

    static async Task LocalMessageTableTransactionNormalTestAsync()
    {
        {
            using var repositoryUnitOfWork =
                Various.SharingPatterns.TimeRange.Use(DbEnum.Order, DateTime.Today).CreateUnitOfWork();

            var orm = repositoryUnitOfWork.Orm;

            await orm.Update<Order>()
                .Set(o => o.Status == "已发货")
                .Where(s => s.Id == 4).ExecuteAffrowsAsync();

            repositoryUnitOfWork.InjectLocalMessageTableEx("OrderDelivery", "您的订单「4」已经发货", governing: "OrderNotice",
                group: "Business1");

            repositoryUnitOfWork.Commit();
        }

        {
            using var repositoryUnitOfWork =
                Various.SharingPatterns.TimeRange.Use(DbEnum.Order, DateTime.Today).CreateUnitOfWork();

            var orm = repositoryUnitOfWork.Orm;

            await orm.Update<Order>()
                .Set(o => o.Status == "已发货")
                .Where(s => s.Id == 4).ExecuteAffrowsAsync();

            repositoryUnitOfWork.InjectLocalMessageTableEx("OrderDelivery", "您的订单「4」已经发货");

            repositoryUnitOfWork.InjectLocalMessageTableEx("ShippingNoticeERP", "ERP订单「4」已经发货");

            repositoryUnitOfWork.Commit();
        }
    }

    #endregion

    #region Test Parallel

    static async Task TestParallelCrossDatabaseTransactionAsync()
    {
        var transactions = Various.SeniorTransactions;

        await Parallel.ForEachAsync(Enumerable.Range(1, 10), async (i, token) =>
        {
            var productId = i;

            var order = Various.SharingPatterns.TimeRange.UseElaborate(DbEnum.Order, DateTime.Today);
          

            // 时间范围分库
            var product = Various.SharingPatterns.Hash.UseElaborate(DbEnum.Product, productId.ToString());

            // 三个不同数据库的事务组合
            using var achieve =
                transactions.CrossDatabaseTransaction.Create("商品扣减,订单创建事务", order, product);
            try
            {
                achieve.Begin();

                var updateProductAffrows = await achieve.Orms.Orm2.Update<Product>().Set(p => p.Price == 22)
                    .Where(p => p.Id == productId)
                    .ExecuteAffrowsAsync(token);

                if (updateProductAffrows == 0)
                {
                    throw new Exception("商品不存在");
                }

                var updateOrderAffrows = await achieve.Orms.Orm1.Update<Order>().Set(o => o.Price == 22)
                    .Where(a => a.ProductId == productId)
                    .ExecuteAffrowsAsync(token);

                if (updateOrderAffrows == 0)
                {
                    throw new Exception("订单不存在");
                }

                achieve.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine("商品扣减,订单创建事务 回滚");
                achieve.Rollback();
            }
        });
    }

    static async Task CrossDatabaseTransactionTestAsync()
    {
        var transactions = Various.SeniorTransactions;

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

    #endregion
}