using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo.Common.Storage.SQLiteModules
{
    [Table("Nep5Transfers")]
    public class Nep5TransferEntity
    {
        [Key]
        public long Id { get; set; }

        public uint BlockHeight { get; set; }

        public string TxId { get; set; }

        [ForeignKey(nameof(TxId))]
        public TransactionEntity Transaction { get; set; }


        public long AssetId { get; set; }
        public ContractEntity Asset { get; set; }

        public long? FromId { get; set; }

        public AddressEntity From { get; set; }

        public long ToId { get; set; }
        public AddressEntity To { get; set; }

        public byte[] Amount { get; set; }


        public DateTime Time { get; set; }

    }
}
