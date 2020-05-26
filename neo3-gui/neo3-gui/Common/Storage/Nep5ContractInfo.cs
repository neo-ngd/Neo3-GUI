using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Common.Storage
{
    public class Nep5ContractInfo
    {
        
        /// <summary>
        /// contract hash
        /// </summary>
        public UInt160 Hash { get; set; }

        public string Name { get; set; }
        public string Symbol { get; set; }
        public byte Decimals { get; set; }


        public DateTime? CreateTime { get; set; }
        public DateTime? DeleteTime { get; set; }
        public DateTime? MigrateTime { get; set; }

        /// <summary>
        /// Create Contract transaction hash string, big-endian without "0x"
        /// </summary>
        public UInt256 CreateTxId { get; set; }


        /// <summary>
        /// Delete or migrate Contract transaction hash string, big-endian without "0x"
        /// </summary>
        public UInt256 DeleteOrMigrateTxId { get; set; }

        public UInt160 MigrateTo { get; set; }
    }
}
