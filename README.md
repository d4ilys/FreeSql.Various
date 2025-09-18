# FreeSql高级管理器

### 安装包

~~~shell
# Package Manager
Install-Package FreeSql.Various

# .NET CLI
dotnet add package FreeSql.Various
~~~

### 初始化管理器

> 创建数据库枚举

~~~C#
public enum DbEnum
{
    Basics,
    Settings,
    /// <summary>
    /// TimeRange分库
    /// </summary>
    Order,
    /// <summary>
    /// Hash分片
    /// </summary>
    Product
}
~~~

> 创建Class继承 FreeSqlVarious<DbEnum>

~~~C#
public class FreeSqlVarious : FreeSqlVarious<DbEnum> { }
~~~

> 单例模式

~~~C#
 public static readonly FreeSqlVarious Various = new FreeSqlVarious();
~~~

ASP.NET Core 通过IServiceCollection创建

~~~C#
builder.Services.AddSingleton<FreeSqlVarious>(provder => {
    var various = new FreeSqlVarious();
    // 注册基础数据库
    Various.Register(DbEnum.Settings, () => new FreeSqlBuilder()
         .UseMonitorCommand(command => VariousConsole.Info<Program>(command.CommandText))
         .UseNoneCommandParameter(true)
         .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("settings")}")
         .Build());
    //注册需要分库的数据库
    .......
    return various;
});
~~~

### 注册数据库

#### 不分库注册方式

> FreeSqlVarious对象支持直接注册数据库

~~~C#
various.Register(DbEnum.Settings, () => new FreeSqlBuilder()
    .UseMonitorCommand(command => VariousConsole.Info<Program>(command.CommandText))
    .UseNoneCommandParameter(true)
    .UseConnectionString(DataType.Sqlite, $"Data Source={GenerateDbPath("settings")}")
    .Build());
~~~

#### 分库模型

`需要说明的是 FreeSql.Various天生支持多租户架构分库模型默认支持多租户，假如系统没有多租户需求，可以创建一个默认租户，方便扩展`

> FreeSqlVarious对象中的SharingPatterns属性支持四种分库方式

##### Tenant「多租户」

~~~C#
//创建Config对象
var tenantConfig = new TenantSharingRegisterConfigure
{
    DatabaseNamingTemplate = "basics_{tenant}"  // {tenant}为约定方式
};

//注册basics相关的租户数据库
tenantConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
{
    Database = "basics_jd",
    BuildIFreeSqlDelegate = () =>
        new FreeSqlBuilder()
            .UseConnectionString(DataType, ConnectionString)
            .Build()
});

tenantConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
{
    Database = "basics_taobao",
    BuildIFreeSqlDelegate = () =>
        new FreeSqlBuilder()
            .UseConnectionString(DataType, ConnectionString)
            .Build()
});

//将配置文件注册到Various
Various.SharingPatterns.Tenant.Register(DbEnum.Basics, tenantConfig);
~~~

##### Hash「分片」

**兼容多租户**

~~~C#
//创建Config对象
var hashConfig = new HashShardingRegisterConfigure
{
    DatabaseNamingTemplate = "product_{tenant}_{slice}"
};
//为不同租户配置分片逻辑
hashConfig.TenantConfigure.Add(new HashShardingRegisterTenantConfigure
{
    Size = 20,  //分片数量 
    TenantMark = "jd" //租户标识
});

hashConfig.TenantConfigure.Add(new HashShardingRegisterTenantConfigure
{
    Size = 10,  //分片数量 
    TenantMark = "taobao" //租户标识
});

//构建product所有相关数据库
foreach (var connection in allDatabaseConnection)
{
    hashConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
    {
        Database = connection.DatabaseName,
        BuildIFreeSqlDelegate = () =>
            new FreeSqlBuilder()
                .UseConnectionString(connection.DataType, connection.ConnectionString)
                .Build()
    });
}

// 注册数据库
Various.SharingPatterns.Hash.Register(dbEnum, hashConfig);
~~~

##### TimeRange「时间分库」

**兼容多租户**

> 分库周期格式为"数值 单位"，如"1 Year"、"1 Month"、"1 Day"

~~~C#
//创建Config对象
var timeRangeConfig = new TimeRangeShardingRegisterConfigure
{
    DatabaseNamingTemplate = "order_{tenant}_{range}"
};

//配置不同租户的分库信息
timeRangeConfig.TenantConfigure.Add(new TimeRangeShardingRegisterTenantConfigure
{
    SharingStartTime = "2024-01-01",  //分库开始时间
    Period = "1 Year",    //分库周期
    TenantMark = "jd"
});

timeRangeConfig.TenantConfigure.Add(new TimeRangeShardingRegisterTenantConfigure
{
    SharingStartTime = "2025-01-01",  //分库开始时间
    Period = "2 Year",    //分库周期
    TenantMark = "taobao"
});

//构建order所有相关数据库
foreach (var connection in allDatabaseConnection)
{
    hashConfig.FreeSqlRegisterItems.Add(new FreeSqlRegisterItem
    {
        Database = connection.DatabaseName,
        BuildIFreeSqlDelegate = () =>
            new FreeSqlBuilder()
                .UseConnectionString(connection.DataType, connection.ConnectionString)
                .Build()
    });
}

// 注册数据库
Various.SharingPatterns.TimeRange.Register(dbEnum, hashConfig);
~~~

##### List「分区」

暂时未实现！！

### 使用数据库

#### 不分库

> 不分库非常简单，通过Use方式即可获得IFreeSql对象

~~~C#
var db = Various.Use(DbEnum.Settings);
db.Insert....
db.Update....
~~~

#### 分库模型

> 因为分库模型默认支持多租户，所以需要设置租户上下文

~~~c#
//TenantContext是基于AsyncLocal实现，在同一个线程中的值都是相同的
//所以在ASP.NET Core中的中间件中设置TenantContext，整个请求的生命周期租户中的值都是相同的
various.TenantContext.Set("taobao");

//ASP.NET Core HTTP Middleware
app.Use(async (context, next) =>
{
    var tenant = context.Request.Headers["Tenant"];
    various.TenantContext.Set(tenant);
    await next();
});
~~~

##### Tenant「多租户」

~~~C#
var db = various.SharingPatterns.TimeRange.Use(DbEnum.Order);
db.Insert....
db.Update....
~~~

##### Hash「分片」

~~~C#
var productId = "";  //分片键
var product = Various.SharingPatterns.Hash.UseElaborate(DbEnum.Product, productId);
db.Insert....
db.Update....
~~~

##### TimeRange「时间分库」

> 按时间分库查询比较的特殊

###### 获取数据库

~~~C#
//根据日期获取数据库
var order = Various.SharingPatterns.TimeRange.UseElaborate(DbEnum.Order, DateTime.Today);

//根据日期范围获取数据库
IEnumerable<IFreeSql> orders = Various.SharingPatterns.TimeRange.Use(DbEnum.Order, DateTime.Parse("2024-01-01"),
      DateTime.Parse("2025-06-01"));
~~~

###### 跨库查询/操作

~~~C#
// 跨库操作
var operationResults = await Various.SharingPatterns.TimeRange.CrossDatabaseOperationAsync(DbEnum.Order,
    DateTime.Parse("2024-01-01"),
    DateTime.Parse("2025-01-02"),
    async (db) => await db.Update<Order>().Set(o=> o.State = 1).Where(o => o.Id = 1).ExecuteAffrowsAsync());

//跨库查询
//并行查询 返回结果无序 需自行排序
var queryResults = await Various.SharingPatterns.TimeRange.CrossDatabaseQueryAsync(DbEnum.Order,
    DateTime.Parse("2024-01-01"),
    DateTime.Parse("2025-01-02"), async db => await db.Select<CrossDatabaseOperationOutcome>()
                                                                                   .Where(o => o.Product = "1").ToListAsync());
~~~



### 跨库统计/分页问题

由于关系型数据库原生不支持分库，所以在分页查询/分组统计的时候会造成很大的麻烦

推荐在使用分库时，使用一个非关系型数据库来冗余一分数据，这里推荐 **ElasticSearch** 

**ElasticSearch** 自带`分片`功能，支持全文索引检索，支持分页和分组查询

> 在TimeRange提供了一个功能
>
> 1.如果查询的时间范围不跨库，则直接走关系型数据库
> 2.如何跨库查询，直接走ElasticSearch查询委托

~~~c#

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
~~~

### 高级事务功能

#### 基于本地消息表的最终一致性

> 初始化调度器

##### 扩展方法

推荐增加一个扩展方法体验更好

~~~C#
/// <summary>
/// 添加本地消息表事务
/// </summary>
/// <param name="freeSqlUnitOfWork">事务单元</param>
/// <param name="taskKey">任务Key</param>
/// <param name="content">传递内容</param>
/// <param name="governing">不同的调度器隔离</param>
/// <param name="group">任务组</param>
/// <param name="activeDo">是否提交后自动触发</param>
/// <returns></returns>
public static LocalMessageTableTransactionUnitOfWorker InjectLocalMessageTableEx(
    this IUnitOfWork freeSqlUnitOfWork, string taskKey, string content, string governing = "",
    string group = "", bool activeDo = true)
{
    var various = FreeSqlVariousInstance.Various;
    return freeSqlUnitOfWork.InjectLocalMessageTable(various, taskKey, content, activeDo, governing, group);
}
~~~

##### 配置调度器

~~~C#
static void LocalMessageTableTransactionDispatchRunningTest()
{
    //准备参与调度的数据库
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
            
            return Task.FromResult(random == 1);
        });

    //注册任务
    localMessageTableTransaction.RegisterTaskExecutor("ShippingNoticeWareHouse", "通知仓库系统",
        content =>
        {
            return Task.FromResult(random == 1);
        });

    localMessageTableTransaction.RegisterTaskExecutor("OrderDelivery", "订单发货通知用户", content =>
    {
		
        return Task.FromResult(random == 1);
    });
    
    //配置任务组调度
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
    
    //自动同步所有参与调度的数据库中的本地消息表
    localMessageTableTransaction.SyncAllDatabaseLocalMessageTable();

    //启动调度器
    localMessageTableTransaction.DispatchRunning();
}
~~~

##### 在事务单元中使用

~~~C#
static async Task LocalMessageTableTransactionNormalTestAsync()
{
    {
        using var repositoryUnitOfWork =
            Various.SharingPatterns.TimeRange.Use(DbEnum.Order, DateTime.Today).CreateUnitOfWork();

        var orm = repositoryUnitOfWork.Orm;

        await orm.Update<Order>()
            .Set(o => o.Status == "已发货")
            .Where(s => s.Id == 4).ExecuteAffrowsAsync();
		
        // 使用扩展方法关联事务
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
~~~

#### 跨服务器的多库事务

和单库事务一样效果相同，出现错误支持全量事务回滚 支持不同关系型数据库 例如 MySql SqlServer 支持数据库存放于不同服务器

原理：

- 在多库事务的开启时，每个库管理开启自己的事务
- 如果某一个库事务开启后的操作出现异常，则回滚全部数据库事务
- 在多库事务提交时，每个库的事务统一提交
- **记录日志**，第一个执行Common的数据库称之为**主库**，会自动创建一个日志表，用于记录多库事务的信息、执行的SQL、业务模块 用于人工介入或者事务补偿
- 如果租户在Common过程中 由于网络等其他原因未成功，所有数据库事务均回滚
- 如果主库Common成功后，其他某一个库可能由于网络原因、数据库宕机 无法Common事务，导致数据不一致，这时候要根据日志进行事务补偿或者人工介入
- 例如 存在三个库（订单库、物流库、商品库） 订单库就是主库（会记录日志） 在Common事务时，如果订单库（主库）Common失败，则（订单库、物流库、商品库）事务全部回滚，如果`订单库`（主库）Common成功，但是`物流库`由于其他原因无法Common成功 则会被日志记录并跳过，然后再去Common `商品库` 以及其他库，然后排查日志根据执行的SQL人工修复数据。

> 如果出现事务不一致，可以去 various_cross_message 查看执行日志，里面保存着 所有事务执行的非查询的SQL

~~~C#
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

        await achieve.Orms.Orm1.Delete<User>().Where(a => true).ExecuteAffrowsAsync();

        await achieve.Orms.Orm2.Delete<Order>().Where(a => true).ExecuteAffrowsAsync();

        await achieve.Orms.Orm3.Delete<Prudct>().Where(a => true).ExecuteAffrowsAsync();

        achieve.Commit();
    }
    catch (Exception e)
    {
        Console.WriteLine("商品扣减,订单创建事务 回滚");
        achieve.Rollback();
    }
}
~~~

