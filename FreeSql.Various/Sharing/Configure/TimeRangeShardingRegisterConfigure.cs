namespace FreeSql.Various;

public class TimeRangeShardingRegisterConfigure
{
    public required string DatabaseNamingTemplate { get; set; }

    public required DateTime StartTime { get; set; }

    public bool IsTenant { get; set; } = false;

    /// <summary>
    /// 分库周期 例如 1 Year, 1 Month, 1 Day
    /// </summary>
    public required string Period { get; set; }

    public IList<FreeSqlRegisterItem> FreeSqlRegisterItems { get; } = new List<FreeSqlRegisterItem>();
}