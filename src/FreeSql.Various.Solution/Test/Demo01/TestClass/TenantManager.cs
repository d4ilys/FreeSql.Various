using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace Demo01.TestClass
{
    internal class TenantManager
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Mark { get; set; }

    }
}