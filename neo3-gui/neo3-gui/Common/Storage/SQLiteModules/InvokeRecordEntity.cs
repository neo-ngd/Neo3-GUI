using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Common.Storage.SQLiteModules
{
    [Table("InvokeRecords")]
    public class InvokeRecordEntity
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id { get; set; }
        public string TxId { get; set; }

        //[ForeignKey(nameof(TxId))]
        public TransactionEntity Tx { get; set; }
        public long ContractId { get; set; }
        public ContractEntity Contract { get; set; }

        /// <summary>
        /// Invoke method names in this transaction, each method only appear once:"transfer,deploy"
        /// </summary>
        public string Methods { get; set; }
    }
}
