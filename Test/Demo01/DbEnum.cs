using FreeSql.Various;

namespace Demo01;

public enum DbEnum
{
    Basics,
    Settings,
    Order,
    Product
}

public class FreeSqlVarious : FreeSqlVarious<DbEnum>
{
}