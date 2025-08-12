using FreeSql.Various;

namespace Demo01;

public enum DbEnum
{
    Basics,
    Order,
    Product
}

public class FreeSqlVarious : FreeSqlVarious<DbEnum>
{
}