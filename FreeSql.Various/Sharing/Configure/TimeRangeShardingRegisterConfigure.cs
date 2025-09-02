namespace FreeSql.Various;

public class TimeRangeShardingRegisterConfigure
{
    public required string DatabaseNamingTemplate { get; set; }

    public IList<TimeRangeShardingRegisterTenantConfigure> TenantConfigure { get; } =
        new List<TimeRangeShardingRegisterTenantConfigure>();

    public IList<FreeSqlRegisterItem> FreeSqlRegisterItems { get; } = new List<FreeSqlRegisterItem>();
}

public class TimeRangeShardingRegisterTenantConfigure
{
    /// <summary>
    /// 分库开始时间
    /// </summary>
    public required DateTime SharingStartTime { get; set; }

    /// <summary>
    /// 分库周期 例如 1 Year, 1 Month, 1 Day
    /// </summary>
    public required string Period { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public required string TenantMark { get; set; }
}