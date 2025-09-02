using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;

namespace Demo01.TestClass
{
    internal class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; }
    }
}