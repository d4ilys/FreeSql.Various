using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.Dashboard
{
    public class FreeSqlVariousDashboardOptions
    {
        public bool Enable { get; set; } = true;

        public string DashboardPath { get; set; } = "VariousDashboard";

        public FreeSqlSchedule FreeSqlSchedule { get; set; }

        public VariousDashboard VariousDashboard { get; set; }

        public List<string> IpWhitelist { get; set; } = new();
    }
}