namespace FreeSql.Various.Contexts;

public class VariousTenantContext
{
    private static readonly AsyncLocal<string> Tenant = new AsyncLocal<string>();

    public void Set(string tenant)
    {
        Tenant.Value = tenant;
    }

    public string Get()
    {
        return Tenant.Value ?? string.Empty;
    }

    public void Clear()
    {
        Tenant.Value = string.Empty;
    }
}