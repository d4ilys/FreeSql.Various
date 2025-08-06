using System.Collections.Concurrent;
using FreeSql.Various.Contexts;
using FreeSql.Various.SeniorTransactions;
using FreeSql.Various.Sharing;
using FreeSql.Various.Utilitys;

namespace FreeSql.Various;

public class FreeSqlVarious<TDbKey> where TDbKey : notnull
{
    private readonly FreeSqlSchedule _schedule;
    private readonly VariousTenantContext _tenantContextContext;

    protected FreeSqlVarious()
    {
        _tenantContextContext = new VariousTenantContext();
        LocalMessageTableScheduling = new LocalMessageTableScheduling();
        _schedule = new FreeSqlSchedule();
        SharingPatterns = new VariousSharingPatterns<TDbKey>(_schedule, _tenantContextContext);
    }

    public IFreeSql Use(TDbKey dbKey)
    {
        var name = dbKey.ToString();
        if (name != null) return _schedule.Get(name);
        throw new Exception($"该数据库[{dbKey}]未注册.");
    }

    public void Register(TDbKey dbKey, Func<IFreeSql> create)
    {
        var name = dbKey.ToString();
        if (name != null) _schedule.Register(name, create);
    }

    public VariousTenantContext TenantContext => _tenantContextContext;

    public VariousSharingPatterns<TDbKey> SharingPatterns { get; }

    public LocalMessageTableScheduling LocalMessageTableScheduling { get; }
}