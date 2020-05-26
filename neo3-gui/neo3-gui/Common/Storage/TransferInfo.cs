using System.Numerics;
using Neo.Models;

namespace Neo.Common.Storage
{
    public class TransferInfo
    {
        public uint BlockHeight { get; set; }

        public UInt256 TxId { get; set; }
        public UInt160 From { get; set; }
        public UInt160 To { get; set; }

        public BigInteger Amount { get; set; }
        public ulong TimeStamp { get; set; }

        public UInt160 Asset { get; set; }
        public AssetInfo AssetInfo { get; set; }
    }
}
