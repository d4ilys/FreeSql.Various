namespace FreeSql.Various;

public class TenantSharingRegisterConfigure
{
    public required string DatabaseNamingTemplate { get; set; }
    
    public IList<FreeSqlRegisterItem> FreeSqlRegisterItems { get; } = new List<FreeSqlRegisterItem>();
    
}