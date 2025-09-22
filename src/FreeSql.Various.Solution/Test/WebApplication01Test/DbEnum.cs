using FreeSql.Various;

namespace WebApplication01Test;

public enum DbEnum
{
    Basics,
    Settings,
    Order,
    /// <summary>
    /// Hash分片
    /// </summary>
    Product
}

public class FreeSqlVarious : FreeSqlVarious<DbEnum>
{
}