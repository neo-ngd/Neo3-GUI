using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Neo.Models.Contracts;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Common.Utility
{
    public class OpCodeConverter
    {
        private static readonly Dictionary<uint, string> _interopServiceMap;
        private static readonly int[] _operandSizePrefixTable = new int[256];
        private static readonly int[] _operandSizeTable = new int[256];


        static OpCodeConverter()
        {
            //初始化所有 InteropService Method
            _interopServiceMap = InteropService.SupportedMethods().ToDictionary(p => p.Hash, p => p.Method);
            //初始化所有 OpCode OperandSize
            foreach (FieldInfo field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute == null) continue;
                int index = (int)(OpCode)field.GetValue(null);
                _operandSizePrefixTable[index] = attribute.SizePrefix;
                _operandSizeTable[index] = attribute.Size;
            }
        }
        public static string ToAsciiString(byte[] byteArray)
        {
            var output = Encoding.UTF8.GetString(byteArray);
            if (output.Any(p => p < '0' || p > 'z')) return byteArray.ToHexString();
            return output;
        }
        public static List<InstructionInfo> Parse(byte[] scripts)
        {
            var result = new List<InstructionInfo>();
            var position = 0;
            while (position < scripts.Length)
            {
                var instruction = new InstructionInfo() { Position = position };
                var op = scripts[position++];
                instruction.OpCode = (OpCode)op;
                var operandSizePrefix = _operandSizePrefixTable[op];
                var operandSize = 0;
                switch (operandSizePrefix)
                {
                    case 0:
                        operandSize = _operandSizeTable[op];
                        break;
                    case 1:
                        operandSize = scripts[position];
                        break;
                    case 2:
                        operandSize = BitConverter.ToUInt16(scripts, position);
                        break;
                    case 4:
                        operandSize = BitConverter.ToInt32(scripts, position);
                        break;
                }
                if (operandSize > 0)
                {
                    position += operandSizePrefix;
                    if (position + operandSize > scripts.Length)
                    {
                        //warning
                        instruction.OpData= new ReadOnlyMemory<byte>(scripts, position,scripts.Length-position).ToArray();
                        result.Add(instruction);
                        return result;
                        //throw new InvalidOperationException();
                    }
                    instruction.OpData = new ReadOnlyMemory<byte>(scripts, position, operandSize).ToArray();
                    if (instruction.OpCode == OpCode.SYSCALL)
                    {
                        instruction.SystemCallMethod = _interopServiceMap[BitConverter.ToUInt32(instruction.OpData)];
                    }
         
                }
                result.Add(instruction);
                position += operandSize;
            }
            return result;
        }

    }
}
