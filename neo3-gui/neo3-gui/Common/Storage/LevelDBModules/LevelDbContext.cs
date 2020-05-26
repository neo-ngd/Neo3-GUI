using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Neo.IO;
using Neo.Models;

namespace Neo.Common.Storage.LevelDBModules
{
    public class LevelDbContext : IDisposable
    {
        private static ConcurrentDictionary<string, DB> _cache = new ConcurrentDictionary<string, DB>();

        private DB _db;
        private string _dbPath;

        private readonly byte[] ExecuteLogPrefix = { 0xff };
        private readonly byte[] SyncIndexPrefix = { 0xfe };
        private readonly byte[] MaxSyncIndexPrefix = { 0xfd };
        private readonly byte[] AssetPrefix = { 0xfc };
        private readonly byte[] BalancePrefix = { 0xfb };
        private readonly byte[] TransferPrefix = { 0xfa };
        private readonly byte[] ContractEventPrefix = { 0xf9 };




        private WriteBatch writeBatch = new WriteBatch();

        static LevelDbContext()
        {
            if (!Directory.Exists("Data_Track"))
            {
                Directory.CreateDirectory("Data_Track");
            }
        }
        public LevelDbContext() : this(Path.Combine("Data_Track", $"TransactionLog_LevelDB_{ProtocolSettings.Default.Magic}"))
        {
        }


        public LevelDbContext(string dbPath)
        {
            _dbPath = dbPath;
            if (_cache.ContainsKey(dbPath))
            {
                _db = _cache[dbPath];
            }
            else
            {
                _db = DB.Open(dbPath, new Options { CreateIfMissing = true });
                _cache[dbPath] = _db;
            }
        }

        private byte[] ContractEventKey(uint height) => ContractEventPrefix.Append(BitConverter.GetBytes(height));

        public Dictionary<UInt256, List<ContractEventInfo>> GetContractEvent(uint height)
        {
            var key = ContractEventKey(height);
            var value = _db.Get(key);
            if (value.NotEmpty())
            {
                var storageValue = value.DeserializeJson<List<ContractEventStorageItem>>();
                return storageValue?.ToDictionary(s => s.TxId, s => s.Events);
            }
            return null;
        }

        /// <summary>
        /// save current block contract change(create,destroy,migrate) info
        /// </summary>
        /// <param name="height"></param>
        /// <param name="value"></param>
        public void SaveContractEvent(uint height, IDictionary<UInt256, List<ContractEventInfo>> value)
        {
            if (value.NotEmpty())
            {
                var key = ContractEventKey(height);
                var storageValue = value.Select(kv => new ContractEventStorageItem
                {
                    TxId = kv.Key,
                    Events = kv.Value,
                });
                writeBatch.Put(key, storageValue.SerializeJsonBytes());
            }
        }


        private byte[] ExecuteLogKey(UInt256 txId) => ExecuteLogPrefix.Append(txId.ToArray());

        /// <summary>
        /// save execute result log after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="log"></param>
        public void SaveExecuteLog(ExecuteResultInfo log)
        {
            if (log != null)
            {
                writeBatch.Put(ExecuteLogKey(log.TxId), log.SerializeJsonBytes());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public ExecuteResultInfo GetExecuteLog(UInt256 txId)
        {
            var value = _db.Get(ReadOptions.Default, ExecuteLogKey(txId));
            if (value.NotEmpty())
            {
                return Encoding.UTF8.GetString(value).DeserializeJson<ExecuteResultInfo>();
            }
            return null;
        }

        private byte[] AssetKey(UInt160 assetId) => AssetPrefix.Append(assetId.ToArray());

        public void SaveAssetInfo(AssetInfo assetInfo)
        {
            var key = AssetKey(assetInfo.Asset);
            var value = _db.Get(key);
            if (value == null)
            {
                writeBatch.Put(key, assetInfo.SerializeJsonBytes());
            }
        }


        public AssetInfo GetAssetInfo(UInt160 assetId)
        {
            var key = AssetKey(assetId);
            var value = _db.Get(ReadOptions.Default, key);
            return value?.DeserializeJson<AssetInfo>();
        }


        private byte[] BalanceKey(UInt160 account, UInt160 asset) => BalancePrefix.Append(account.ToArray(), asset.ToArray());
        public void UpdateBalance(UInt160 account, UInt160 asset, BigInteger balance, uint height)
        {
            var key = BalanceKey(account, asset);
            //var value = GetBalance(key);
            //if (value?.Height >= height)
            //{
            //    return;
            //}
            var balanceRecord = new BalanceStorageItem() { Balance = balance, Height = height };
            writeBatch.Put(key, balanceRecord.SerializeJsonBytes());
        }


        public BalanceStorageItem GetBalance(UInt160 account, UInt160 asset)
        {
            var key = BalanceKey(account, asset);
            var value = GetBalance(key);
            return value;
        }

        private BalanceStorageItem GetBalance(byte[] balanceKey)
        {
            var value = _db.Get(balanceKey);
            if (value.NotEmpty())
            {
                return value.DeserializeJson<BalanceStorageItem>();
            }
            return null;
        }


        private byte[] TransferKey(UInt256 txId) => TransferPrefix.Append(txId.ToArray());


        /// <summary>
        /// will save after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="transfers"></param>
        public void SaveTransfers(UInt256 txId, List<TransferInfo> transfers)
        {
            var key = TransferKey(txId);
            writeBatch.Put(key, transfers.Select(t => new TransferStorageItem()
            {
                From = t.From,
                To = t.To,
                Amount = t.Amount,
                Asset = t.Asset,
            }).SerializeJsonBytes());
        }


        public List<TransferStorageItem> GetTransfers(UInt256 txId)
        {
            var key = TransferKey(txId);
            var value = _db.Get(key);
            if (value.NotEmpty())
            {
                return value.DeserializeJson<List<TransferStorageItem>>();
            }
            return null;
        }


        //private byte[] SyncIndexKey(byte[] db, uint height) => SyncIndexPrefix.Append(db, BitConverter.GetBytes(height));
        private byte[] MaxSyncIndexKey(byte[] db) => MaxSyncIndexPrefix.Append(db);

        /// <summary>
        /// save synced height after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="dbPrefix"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool AddSyncIndex(byte[] dbPrefix, uint height)
        {
            //writeBatch.Put(SyncIndexKey(dbPrefix, height), new Byte[] { 1 });
            //_db.Put(WriteOptions.Default, SyncIndexKey(dbPrefix, height), new Byte[] { 1 });
            SetMaxSyncIndex(dbPrefix, height);
            return true;
        }

        /// <summary>
        /// is this height synced? true:yes
        /// </summary>
        /// <param name="dbPrefix"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool HasSyncIndex(byte[] dbPrefix, uint height)
        {
            var max = GetMaxSyncIndex(dbPrefix);
            return max >= height;
            //var value = _db.Get(ReadOptions.Default, SyncIndexKey(dbPrefix, height));
            //return value != null;
        }

        /// <summary>
        /// save synced max height after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="dbPrefix"></param>
        /// <param name="height"></param>
        public void SetMaxSyncIndex(byte[] dbPrefix, uint height)
        {
            var max = GetMaxSyncIndex(dbPrefix);
            if (max == null || max < height)
            {
                writeBatch.Put(MaxSyncIndexKey(dbPrefix), BitConverter.GetBytes(height));
                //_db.Put(WriteOptions.Default, MaxSyncIndexKey(dbPrefix), BitConverter.GetBytes(height));
            }
        }

        public uint? GetMaxSyncIndex(byte[] dbPrefix)
        {
            var max = _db.Get(ReadOptions.Default, MaxSyncIndexKey(dbPrefix));
            if (max == null)
            {
                return null;
            }
            return BitConverter.ToUInt32(max);
        }


        public List<uint> ListSyncIndex(byte[] dbPrefix)
        {
            var result = _db.Find(ReadOptions.Default, SyncIndexPrefix.Concat(dbPrefix).ToArray(), (key, value) => key.Skip(17)).ToList();

            return result.Select(r => BitConverter.ToUInt32(r.ToArray())).ToList();
        }


        public void Commit()
        {
            _db.Write(WriteOptions.Default, writeBatch);
            ResetWriteBatch();
        }

        private void ResetWriteBatch()
        {
            writeBatch?.Clear();
            writeBatch = new WriteBatch();
        }

        public void Dispose()
        {
            writeBatch?.Clear();
        }
    }
}
