using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Common.Utility
{
    public class OpCodeConverter
    {
        public static string ToAsciiString(byte[] byteArray)
        {
            var output = Encoding.UTF8.GetString(byteArray);
            if (output.Any(p => p < '0' || p > 'z')) return byteArray.ToHexString();
            return output;
        }
        public static List<string> Analysis(List<byte> scripts, bool raw)
        {
            //初始化所有 OpCode
            var OperandSizePrefixTable = new int[256];
            var OperandSizeTable = new int[256];
            foreach (FieldInfo field in typeof(OpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<OperandSizeAttribute>();
                if (attribute == null) continue;
                int index = (int)(OpCode)field.GetValue(null);
                OperandSizePrefixTable[index] = attribute.SizePrefix;
                OperandSizeTable[index] = attribute.Size;
            }
            //初始化所有 InteropService
            var dic = new Dictionary<uint, string>();
            InteropService.SupportedMethods().ToList().ForEach(p => dic.Add(p.Hash, p.Method));

            //解析 Scripts
            var result = new List<string>();
            while (scripts.Count > 0)
            {
                var op = (OpCode)scripts[0];
                var operandSizePrefix = OperandSizePrefixTable[scripts[0]];
                var operandSize = OperandSizeTable[scripts[0]];
                scripts.RemoveAt(0);

                if (operandSize > 0)
                {
                    var operand = scripts.Take(operandSize).ToArray();
                    if (op.ToString().StartsWith("PUSHINT"))
                    {
                        result.Add(raw ? $"{op.ToString()} {operand.ToHexString()}" : $"{op.ToString()} {new BigInteger(operand)}");
                    }
                    else if (op == OpCode.SYSCALL)
                    {
                        result.Add(raw ? $"{op.ToString()} {operand.ToHexString()}" : $"{op.ToString()} {dic[BitConverter.ToUInt32(operand)]}");
                    }
                    else
                    {
                        result.Add($"{op.ToString()} {operand.ToHexString()}");
                    }
                    scripts.RemoveRange(0, operandSize);
                }
                if (operandSizePrefix > 0)
                {
                    var number = (int)new BigInteger(scripts.Take(operandSizePrefix).ToArray());
                    scripts.RemoveRange(0, operandSizePrefix);
                    var operand = scripts.Take(number).ToArray();

                    result.Add(raw ? $"{op.ToString()} LENGTH:{number} {operand.ToHexString()}" : $"{op.ToString()} {(number == 20 ? new UInt160(operand).ToString() : ToAsciiString(operand))}");
                    scripts.RemoveRange(0, number);
                }
            }
            return result;
        }
    }
}
