using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Common.Storage.SQLiteModules;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Common.Storage
{
    public class ExecuteResultInfo
    {
        /// <summary>
        /// transaction hash
        /// </summary>
        public UInt256 TxId { get; set; }

        public TriggerType Trigger { get; set; }
        public VMState VMState { get; set; }
        public long GasConsumed { get; set; }

        /// <summary>
        /// execute result json array
        /// </summary>
        public JObject ResultStack { get; set; }
        public List<NotificationInfo> Notifications { get; set; }
    }

    public class NotificationInfo
    {
        /// <summary>
        /// contract script hash bin-endian, starts with "0x"
        /// </summary>
        public string Contract { get; set; }

        public JObject State { get; set; }
    }
}
