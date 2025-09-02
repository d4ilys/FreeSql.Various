namespace FreeSql.Various;

public class HashShardingRegisterConfigure
{
    public required string DatabaseNamingTemplate { get; set; }

    public IList<HashShardingRegisterTenantConfigure> TenantConfigure { get; } = new List<HashShardingRegisterTenantConfigure>();

    public IList<FreeSqlRegisterItem> FreeSqlRegisterItems { get; } = new List<FreeSqlRegisterItem>();
}

public class HashShardingRegisterTenantConfigure
{
    /// <summary>
    /// 分片数量
    /// </summary>
    public required int Size { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public required string TenantMark { get; set; }
}