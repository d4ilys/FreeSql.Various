using System.Collections.Concurrent;
using FreeSql.Various.Models;

namespace FreeSql.Various;

public class FreeSqlSchedule
{
    private readonly IdleBus<string, FreeSqlElaborate> _idleBus = new(TimeSpan.FromMinutes(5));

    public FreeSqlElaborate Get(string key)
    {
        return !_idleBus.Exists(key) ? throw new Exception($"该数据库[{key}]未注册.") : _idleBus.Get(key);
    }

    public IdleBus<string, FreeSqlElaborate> IdleBus()
    {
        return _idleBus;
    }

    public bool Register(string key, Func<FreeSqlElaborate> func)
    {
        return _idleBus.TryRegister(key, func);
    }

    public bool IsRegistered(string key)
    {
        return _idleBus.Exists(key);
    }
}