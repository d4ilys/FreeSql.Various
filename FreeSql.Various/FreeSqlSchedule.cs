using System.Collections.Concurrent;
using FreeSql.Various.Models;

namespace FreeSql.Various;

public class FreeSqlSchedule
{
    private readonly IdleBus<string, IFreeSql> _idleBus = new(TimeSpan.FromMinutes(10));

    public IFreeSql Get(string key)
    {
        if (!_idleBus.Exists(key))
        {
            throw new Exception($"该数据库[{key}]未注册.");
        }

        return _idleBus.Get(key);
    }

    public IdleBus<string, IFreeSql> GetIdleBus()
    {
        return _idleBus;
    }

    public bool Register(string key, Func<IFreeSql> func)
    {
        return _idleBus.TryRegister(key, func);
    }

    public bool IsRegistered(string key)
    {
        return _idleBus.Exists(key);
    }
}