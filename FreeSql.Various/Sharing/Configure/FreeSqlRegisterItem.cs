using System.Diagnostics.CodeAnalysis;

namespace FreeSql.Various;

public class FreeSqlRegisterItem
{
    public FreeSqlRegisterItem()
    {
    }

    [SetsRequiredMembers]
    public FreeSqlRegisterItem(string database, Func<IFreeSql> buildIFreeSqlDelegate)
    {
        Database = database;
        BuildIFreeSqlDelegate = buildIFreeSqlDelegate;
    }

    public required string Database { get; set; }

    public required Func<IFreeSql> BuildIFreeSqlDelegate { get; set; }
}