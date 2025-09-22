using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.Dashboard
{
    public class VariousDashboard
    {
        public IList<VariousDashboardCustomExecutor> CustomExecutors { get; set; } =
            new List<VariousDashboardCustomExecutor>();

    }
}