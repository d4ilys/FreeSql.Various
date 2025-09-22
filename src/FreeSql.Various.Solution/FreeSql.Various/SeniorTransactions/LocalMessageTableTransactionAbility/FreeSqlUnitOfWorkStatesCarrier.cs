using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.Various.SeniorTransactions.LocalMessageTableTransactionAbility
{
    internal class FreeSqlUnitOfWorkStatesCarrier
    {
        internal string Group { get; set; }

        internal string Governing { get; set; }

        internal LocalMessageTableTransactionUnitOfWorker UnitOfWorker { get; set; }
    }
}
