namespace FreeSql.Various.Models;

public class FreeSqlElaborate<TDbKey>
{
    /// <summary>
    /// 数据库键
    /// </summary>
    public required TDbKey DbKey { get; set; }

    /// <summary>
    /// IFreeSql对象
    /// </summary>
    public required IFreeSql FreeSql { get; set; }

    /// <summary>
    /// 数据库名
    /// </summary>
    public required string Database { get; set; }
}