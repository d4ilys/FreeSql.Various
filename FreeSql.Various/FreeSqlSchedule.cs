using System.Collections.Concurrent;
using FreeSql.Various.Models;

namespace FreeSql.Various;

public class FreeSqlSchedule
{
    private readonly IdleBus<string, IFreeSql> _idleBus = new();

    public IFreeSql Get(string key)
    {
        if (!_idleBus.Exists(key))
        {
            throw new Exception($"该数据库[{key}]未注册.");
        }

        return _idleBus.Get(key);
    }

    public bool Register(string key, Func<IFreeSql> func)
    {
        return _idleBus.TryRegister(key, func);
    }
}