using FreeSql.Various;

namespace Demo01;

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