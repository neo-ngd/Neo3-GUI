using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IO;

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

        private WriteBatch writeBatch = new WriteBatch();

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


        private byte[] ExecuteLogKey(UInt256 txId) => ExecuteLogPrefix.Concat(txId.ToArray()).ToArray();

        /// <summary>
        /// save log after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="log"></param>
        public void AddExecuteLog(UInt256 txId, ExecuteResultInfo log)
        {
            if (log != null)
            {
                writeBatch.Put(ExecuteLogKey(txId), log.SerializeJsonBytes());
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




        private byte[] SyncIndexKey(byte[] db, uint height) => SyncIndexPrefix.Concat(db).Concat(BitConverter.GetBytes(height)).ToArray();
        private byte[] MaxSyncIndexKey(byte[] db) => MaxSyncIndexPrefix.Concat(db).ToArray();

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
