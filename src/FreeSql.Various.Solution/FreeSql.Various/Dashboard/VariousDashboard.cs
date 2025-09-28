using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.Dashboard
{
    public class VariousDashboard
    {
        public void RegisterCustomExecutor(string name, Action<IList<VariousDashboardCustomExecutor>> action)
        {
            IList<VariousDashboardCustomExecutor> executors;
            //判断是否存在
            if (!CustomExecutors.ContainsKey(name))
            {
                executors = new List<VariousDashboardCustomExecutor>();
                action(executors);
                CustomExecutors[name] = executors;
            }
            else
            {
                executors = CustomExecutors[name];
                CustomExecutors[name] = executors;
            }
        }

        public Dictionary<string, IList<VariousDashboardCustomExecutor>> CustomExecutors = new();
    }
}