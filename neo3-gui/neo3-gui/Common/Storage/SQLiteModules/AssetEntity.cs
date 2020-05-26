using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neo.Common.Storage.SQLiteModules
{
    [Table("Asset")]
    public class AssetEntity
    {
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// nep5 asset script hash byte array, little-endian
        /// </summary>
        public byte[] Hash { get; set; }

        /// <summary>
        /// nep5 asset script hash string, big-endian without "Ox"
        /// </summary>
        public string Asset { get; set; }

        public string Name { get; set; }
        public string Symbol { get; set; }
        public byte Decimals { get; set; }

        public byte[] TotalSupply { get; set; }
     
    }
}