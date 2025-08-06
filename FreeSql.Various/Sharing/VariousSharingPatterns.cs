using FreeSql.Various.Contexts;
using FreeSql.Various.Sharing.Pattern;

namespace FreeSql.Various.Sharing;

public class VariousSharingPatterns<TDbKey>(FreeSqlSchedule schedule, VariousTenantContext tenantContext) where TDbKey : notnull
{
    /// <summary>
    /// 时间范围分库
    /// </summary>
    public TimeRangeSharingPattern<TDbKey> TimeRange => new TimeRangeSharingPattern<TDbKey>(schedule, tenantContext);


    /// <summary>
    /// 哈希分库
    /// </summary>
    public HashSharingPattern<TDbKey> Hash => new HashSharingPattern<TDbKey>();


    /// <summary>
    /// 列表分库
    /// </summary>
    public ListSharingPattern<TDbKey> List => new ListSharingPattern<TDbKey>();

    /// <summary>
    /// 租户分库
    /// </summary>
    public TenantSharingPattern<TDbKey> Tenant => new TenantSharingPattern<TDbKey>(schedule, tenantContext);
}