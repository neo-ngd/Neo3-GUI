using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.Common.Storage;
using Neo.Common.Storage.SQLiteModules;
using Neo.Extensions;
using Neo.Models;
using Neo.SmartContract.Native;

namespace neo3_gui.tests.Storage
{
    [TestClass]
    public class TrackDB_Test
    {
        private TrackDB _db = new TrackDB();



        /// <summary>
        /// Test for adding a transfer record to TrackDB.
        /// 
        /// Fixes applied for Neo 3.9.1 compatibility:
        /// 
        /// 1. Contract must be created first:
        ///    - AddTransfer requires GetActiveContract() to return a valid ContractEntity.
        ///    - Without the contract, asset.Id would be null, causing NullReferenceException.
        /// 
        /// 2. TxId must be a valid UInt256, not empty string:
        ///    - Changed from: TxId = UInt256.Parse("")  // Throws FormatException in Neo 3.9.1
        ///    - Changed to: TxId = UInt256.Zero   // Valid zero hash
        /// </summary>
        [TestMethod]
        public async Task AddTransfer_Test()
        {
            // Fix 1: Create contract first to avoid NullReferenceException in AddTransfer
            _db.CreateContract(new ContractEntity
            {
                Hash = NativeContract.NEO.Hash.ToBigEndianHex(),
                Name = "NEO",
                Symbol = "NEO",
                Decimals = NativeContract.NEO.Decimals,
                AssetType = AssetType.Nep17,
                CreateTime = DateTime.UtcNow,
                CreateTxId = UInt256.Zero.ToBigEndianHex()
            });

            var transfer = new TransferInfo()
            {
                BlockHeight = 0,
                From = UInt160.Parse("0xf9df308b7bb380469354062f6b73f9cb0124317b"),
                To = UInt160.Parse("0x18c52425debcc3c76c06c3368044b8c60609a904"),
                    
                Asset = NativeContract.NEO.Hash,
                Amount = 1,
                // Fix 2: Use UInt256.Zero instead of UInt256.Parse("") to avoid FormatException
                TxId = UInt256.Zero,
                TimeStamp = DateTime.Now.ToTimestamp(),
                //AssetInfo = new AssetInfo()
                //{
                //    Asset = NativeContract.NEO.Hash,
                //    Name = "NEO",
                //    Symbol = "neo",
                //    Decimals = NativeContract.NEO.Decimals,
                //},
            };

            _db.AddTransfer(transfer);
            //_db.Commit();
        }
    }
}
