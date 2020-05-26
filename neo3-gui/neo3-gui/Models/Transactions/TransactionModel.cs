using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;

namespace Neo.Models.Transactions
{
    public class TransactionModel
    {
        public TransactionModel(Transaction tx)
        {
            TxId = tx.Hash;
            NetworkFee = new BigDecimal(tx.NetworkFee, NativeContract.GAS.Decimals);
            Nonce = tx.Nonce;
            Script = tx.Script;
            Sender = tx.Sender;
            SystemFee = new BigDecimal(tx.SystemFee, NativeContract.GAS.Decimals);
            ValidUntilBlock = tx.ValidUntilBlock;
            Version = tx.Version;
            Size = tx.Size;
            Attributes = tx.Attributes?.Select(a => new TranAttributeModel()
            {
                Usage = a.Usage,
                Data = a.Data,
            }).ToList();
            Witnesses = tx.Witnesses?.Select(w => new WitnessModel(w)
            ).ToList();
        }

        public UInt256 TxId { get; set; }
        public UInt256 BlockHash { get; set; }
        public uint? BlockHeight { get; set; }
        public DateTime? BlockTime => Timestamp?.FromTimestampMS().ToLocalTime();
        public ulong? Timestamp { get; set; }
        public List<TransferModel> Transfers { get; set; }
        public BigDecimal NetworkFee { get; set; }

        public uint Nonce { get; set; }

        public byte[] Script { get; set; }

        public UInt160 Sender { get; set; }

        public BigDecimal SystemFee { get; set; }

        public uint ValidUntilBlock { get; set; }

        public byte Version { get; set; }


        public uint? Confirmations { get; set; }

        public int Size { get; set; }

        public List<TranAttributeModel> Attributes { get; set; }
        public List<WitnessModel> Witnesses { get; set; }

        public List<NotifyModel> Notifies { get; set; } = new List<NotifyModel>();

    }
}
