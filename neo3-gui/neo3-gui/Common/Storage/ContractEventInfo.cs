using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Common.Storage
{
    public class ContractEventInfo
    {
        public UInt160 Contract { get; set; }

        public ContractEventType Event { get; set; }

        public UInt160 MigrateToContract { get; set; }
    }
}
