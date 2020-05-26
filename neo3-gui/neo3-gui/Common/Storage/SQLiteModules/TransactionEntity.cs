using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Common.Storage.SQLiteModules
{
    [Table("Transaction")]

    public class TransactionEntity
    {
        [Key]
        //public long Id { get; set; }
        public string TxId { get; set; }
        public uint BlockHeight { get; set; }

        public long? SenderId { get; set; }

        public AddressEntity Sender { get; set; }

        public DateTime Time { get; set; }
        public IList<Nep5TransferEntity> Transfers { get; set; }
        public IList<InvokeRecordEntity> InvokeContracts { get; set; }
    }
}
