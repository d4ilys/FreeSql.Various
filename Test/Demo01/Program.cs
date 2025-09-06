using Demo01.Tables;
using Demo01.TestClass;
using FreeSql.Various;
using FreeSql.Various.Sharing;
using static Demo01.FreeSqlVariousInstance;

namespace Demo01;

class Program
{
    static async Task Main(string[] args)
    {
        await DatabaseInitialize.InitializeAsync();
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
}