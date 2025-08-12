namespace FreeSql.Various;

public class HashShardingRegisterConfigure 
{
    public required string DatabaseNamingTemplate { get; set; }
    
    public required bool IsTenant { get; set; }

    public required int Size { get; set; } 
    
    public IList<FreeSqlRegisterItem> FreeSqlRegisterItems { get; } = new List<FreeSqlRegisterItem>();
    
}