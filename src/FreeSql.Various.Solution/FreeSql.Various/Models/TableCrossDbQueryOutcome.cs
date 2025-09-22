namespace FreeSql.Various;

public class TableCrossDbQueryOutcome<TResult>(List<TResult> data, long total)
{
    public List<TResult> Data { get; set; } = data;

    public long Total { get; set; } = total;
}