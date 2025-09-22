namespace FreeSql.Various.Models;

public class FreeSqlElaborate<TDbKey> : FreeSqlElaborate
{
    /// <summary>
    /// 数据库键
    /// </summary>
    public required TDbKey DbKey { get; set; }
}

public class FreeSqlElaborate: IDisposable
{
    /// <summary>
    /// IFreeSql对象
    /// </summary>
    public required IFreeSql FreeSql { get; set; }

    /// <summary>
    /// 数据库名
    /// </summary>
    public required string Database { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    public void Dispose()
    {
        FreeSql.Dispose();
    }
}