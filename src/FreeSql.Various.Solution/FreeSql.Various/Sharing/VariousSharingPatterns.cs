using FreeSql.Various.Contexts;
using FreeSql.Various.Sharing.Pattern;

namespace FreeSql.Various.Sharing;

/// <summary>
/// 支持多种分库模式
/// </summary>
/// <typeparam name="TDbKey"></typeparam>
public class VariousSharingPatterns<TDbKey>(FreeSqlVarious<TDbKey> various) where TDbKey : notnull
{
    /// <summary>
    /// 时间范围分库
    /// </summary>
    public TimeRangeSharingPattern<TDbKey> TimeRange => new TimeRangeSharingPattern<TDbKey>(various.Schedule, various.TenantContext);


    /// <summary>
    /// 哈希分库
    /// </summary>
    public HashSharingPattern<TDbKey> Hash => new HashSharingPattern<TDbKey>(various.Schedule, various.TenantContext);


    /// <summary>
    /// 列表分库
    /// </summary>
    public ListSharingPattern<TDbKey> List => new ListSharingPattern<TDbKey>();

    /// <summary>
    /// 租户分库
    /// </summary>
    public TenantSharingPattern<TDbKey> Tenant => new TenantSharingPattern<TDbKey>(various.Schedule, various.TenantContext);
}