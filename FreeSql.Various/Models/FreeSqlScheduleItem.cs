namespace FreeSql.Various.Models;

internal class FreeSqlScheduleItem<TDbKey>
{
    public required TDbKey DbKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public Action<IFreeSql> Action { get; set; } = null!;
}