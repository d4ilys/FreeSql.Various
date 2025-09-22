namespace FreeSql.Various;

public class CrossDatabaseOperationOutcome
{
    public required string DatabaseName { get; set; }
    public bool Success { get; set; }
    
    public string? ErrorMessage { get; set; }
}