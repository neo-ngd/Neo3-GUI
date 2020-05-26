using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.VM;

namespace Neo.Models.Contracts
{
    public class InstructionInfo
    {
        public int Position { get; set; }
        public OpCode OpCode { get; set; }

        public string OpCodeName => OpCode.ToString();

        public byte[] OpData { get; set; }

        /// <summary>
        /// only exists when <see cref="OpCode"/> is OpCode.SYSCALL
        /// </summary>
        public string SystemCallMethod { get; set; }

        public string OpDataUtf8String => OpData == null ? null : Encoding.UTF8.GetString(OpData);
    }
}
