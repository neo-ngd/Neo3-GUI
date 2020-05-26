using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neo.Common.Storage.LevelDBModules;
using Neo.Common.Storage.SQLiteModules;
using Neo.IO;
using Neo.Models;

namespace Neo.Common.Storage
{
    public class TrackDB : IDisposable
    {
        private readonly uint _magic;
        private readonly SQLiteContext _sqldb;
        private readonly LevelDbContext _leveldb;
        private readonly DateTime _createTime = DateTime.Now;

        public TimeSpan LiveTime => DateTime.Now - _createTime;

        private static bool _hasConsistencyCheck = false;

        public TrackDB()
        {
            _magic = ProtocolSettings.Default.Magic;
            if (!Directory.Exists("Data_Track"))
            {
                Directory.CreateDirectory("Data_Track");
            }
            _sqldb = new SQLiteContext(Path.Combine($"Data_Track", $"track.{_magic}.db"));

            var levelDbPath = Path.Combine("Data_Track", $"TransactionLog_LevelDB_{_magic}");
            if (!Directory.Exists(levelDbPath))
            {
                Directory.CreateDirectory(levelDbPath);
            }
            _leveldb = new LevelDbContext(levelDbPath);

            if (!_hasConsistencyCheck)
            {
                _hasConsistencyCheck = true;
                InitConsistencyCheck();
            }
        }


        /// <summary>
        /// check 2 db has the same sync index
        /// </summary>
        public void InitConsistencyCheck()
        {
            var levelMax = _leveldb.GetMaxSyncIndex(_sqldb.Identity);
            var sqlMax = _sqldb.GetMaxSyncIndex();
            if (levelMax == sqlMax)
            {
                return;
            }
            if ((levelMax == null && sqlMax == 0) || levelMax < sqlMax)
            {
                // try repair sync height
                // when last sync: sqldb save successfully, but leveldb fails
                _leveldb.AddSyncIndex(_sqldb.Identity, sqlMax.Value);
                _leveldb.Commit();
                return;
            }
            //unknown issue?
            throw new Exception("track db damaged!");
        }

        public void Commit()
        {
            _sqldb.SaveChanges();
            _leveldb.Commit();
        }

        #region SyncIndex

        public void AddSyncIndex(uint index)
        {
            _leveldb.AddSyncIndex(_sqldb.Identity, index);
            _sqldb.SyncIndexes.Add(new SyncIndex() { BlockHeight = index });
        }

        public bool HasSyncIndex(uint index)
        {
            return _leveldb.HasSyncIndex(_sqldb.Identity, index);
            //return _sqldb.SyncIndexes.AsNoTracking().FirstOrDefault(s => s.BlockHeight == index) != null;
        }


        public uint? GetMaxSyncIndex()
        {
            return _leveldb.GetMaxSyncIndex(_sqldb.Identity);
        }

        #endregion


        #region ExecuteResult


        /// <summary>
        /// save log only after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="log"></param>
        public void AddExecuteLog(UInt256 txId, ExecuteResultInfo log)
        {
            _leveldb.AddExecuteLog(txId, log);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public ExecuteResultInfo GetExecuteLog(UInt256 txId)
        {
            return _leveldb.GetExecuteLog(txId);
        }


        #endregion

        #region transfer


        /// <summary>
        /// will save after call <see cref="Commit"/> method
        /// </summary>
        /// <param name="newTransaction"></param>
        public void AddTransfer(TransferInfo newTransaction)
        {
            var from = GetOrCreateAddress(newTransaction.From);
            var to = GetOrCreateAddress(newTransaction.To);
            var asset = GetOrCreateAsset(newTransaction.AssetInfo);

            var tran = new Nep5TransactionEntity
            {
                BlockHeight = newTransaction.BlockHeight,
                TxId = newTransaction.TxId.ToBigEndianHex(),
                FromId = from?.Id,
                ToId = to.Id,

                Amount = newTransaction.Amount.ToByteArray(),
                AssetId = asset.Id,
                Time = newTransaction.TimeStamp.FromTimestampMS(),
            };
            _sqldb.Nep5Transactions.Add(tran);
        }


        /// <summary>
        /// update record will save after call <see cref="Commit"/> method;
        /// new record will save immediately
        /// </summary>
        /// <param name="addressHash"></param>
        /// <param name="assetInfo"></param>
        /// <param name="balance"></param>
        /// <param name="height"></param>
        public void UpdateBalance(UInt160 addressHash, AssetInfo assetInfo, BigInteger balance, uint height)
        {
            if (addressHash == null) return;
            var address = GetOrCreateAddress(addressHash);
            var asset = GetOrCreateAsset(assetInfo);
            var balanceRecord = GetOrCreateBalance(address, asset, balance, height);

            if (balanceRecord.BlockHeight >= height)
            {
                //no need update
                return;
            }
            balanceRecord.Balance = balance.ToByteArray();
            balanceRecord.BlockHeight = height;
        }


        /// <summary>
        ///  Paged by Transactions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public PageList<TransactionInfo> FindTransactions(TransactionFilter filter)
        {
            IQueryable<TransactionEntity> query = _sqldb.Transactions.Include(tx => tx.Sender);

            if (filter.StartTime != null)
            {
                query = query.Where(r => r.Time >= filter.StartTime.Value.ToUniversalTime());
            }
            if (filter.EndTime != null)
            {
                query = query.Where(r => r.Time <= filter.EndTime.Value.ToUniversalTime());
            }

            if (filter.BlockHeight != null)
            {
                query = query.Where(r => r.BlockHeight == filter.BlockHeight);
            }
            if (filter.TxIds.NotEmpty())
            {
                var txids = filter.TxIds.Select(t => t.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(r => txids.Contains(r.TxId));
            }

            var pageList = new PageList<TransactionInfo>();
            var pageIndex = filter.PageIndex <= 0 ? 0 : filter.PageIndex - 1;
            pageList.TotalCount = query.Count();
            pageList.PageIndex = pageIndex + 1;
            pageList.PageSize = filter.PageSize;
            if (filter.PageSize > 0)
            {
                pageList.List.AddRange(query.OrderByDescending(g => g.Time)
                    .Skip(pageIndex * filter.PageSize)
                    .Take(filter.PageSize).Select(tx => new TransactionInfo()
                    {
                        TxId = UInt256.Parse(tx.TxId),
                        BlockHeight = tx.BlockHeight,
                        Sender = tx.Sender != null ? UInt160.Parse(tx.Sender.Address) : null,
                        Time = tx.Time.AsUtcTime(),
                    }));
            }
            return pageList;
        }

        /// <summary>
        ///  Paged by Transactions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public PageList<TransferInfo> FindNep5Transactions(TransferFilter filter)
        {
            var query = BuildQuery(filter);
            var pageList = new PageList<TransferInfo>();
            var pageIndex = filter.PageIndex <= 0 ? 0 : filter.PageIndex - 1;
            pageList.TotalCount = query.GroupBy(q => q.TxId).Count();
            pageList.PageIndex = pageIndex + 1;
            pageList.PageSize = filter.PageSize;
            if (filter.PageSize > 0)
            {
                var txIds = query.GroupBy(q => new { q.TxId, q.Time }).OrderByDescending(g => g.Key.Time).Select(g => g.Key)
                    .Skip(pageIndex * filter.PageSize)
                    .Take(filter.PageSize).Select(g => g.TxId).ToList();
                pageList.List.AddRange(query.Where(q => txIds.Contains(q.TxId)).OrderByDescending(r => r.Time).ToList().Select(ToNep5TransferInfo));
            }
            return pageList;
        }


        /// <summary>
        /// Paged by transfer
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public PageList<TransferInfo> FindTransfer(TransferFilter filter)
        {
            var query = BuildQuery(filter);
            var pageList = new PageList<TransferInfo>();
            var pageIndex = filter.PageIndex <= 0 ? 0 : filter.PageIndex - 1;
            pageList.TotalCount = query.Count();
            pageList.PageIndex = pageIndex + 1;
            pageList.PageSize = filter.PageSize;
            if (filter.PageSize > 0)
            {
                pageList.List.AddRange(query.OrderByDescending(r => r.Time).Skip(pageIndex * filter.PageSize)
                    .Take(filter.PageSize).ToList().Select(ToNep5TransferInfo));
            }
            return pageList;
        }


        /// <summary>
        /// query balances
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<BalanceInfo> FindAssetBalance(BalanceFilter filter)
        {
            IQueryable<AssetBalanceEntity> query = _sqldb.AssetBalances.Include(a => a.Address).Include(a => a.Asset);
            if (filter.Addresses.NotEmpty())
            {
                var addrs = filter.Addresses.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(q => addrs.Contains(q.Address.Address));
            }

            if (filter.Assets.NotEmpty())
            {
                var assets = filter.Assets.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(q => assets.Contains(q.Asset.Asset));
            }

            var balances = query.ToList();
            return balances.Select(b => new BalanceInfo()
            {
                Address = UInt160.Parse(b.Address.Address),
                Asset = UInt160.Parse(b.Asset.Asset),
                AssetName = b.Asset.Name,
                AssetSymbol = b.Asset.Symbol,
                AssetDecimals = b.Asset.Decimals,
                Balance = new BigInteger(b.Balance),
                BlockHeight = b.BlockHeight,
            });
        }


        public IEnumerable<AssetEntity> GetAllAssets()
        {
            return _sqldb.Assets.ToList();
        }

        #endregion

        #region Transaction

        public void AddTransaction(TransactionInfo transaction)
        {
            var sender = GetOrCreateAddress(transaction.Sender);
            _sqldb.Transactions.Add(new TransactionEntity()
            {
                TxId = transaction.TxId.ToBigEndianHex(),
                BlockHeight = transaction.BlockHeight,
                Time = transaction.Time,
                SenderId = sender?.Id,
            });
        }

        #endregion



        #region Private


        private readonly Dictionary<UInt160, AddressEntity> _addressCache = new Dictionary<UInt160, AddressEntity>();
        private AddressEntity GetOrCreateAddress(UInt160 address)
        {
            if (address == null) return null;
            if (_addressCache.ContainsKey(address))
            {
                return _addressCache[address];
            }
            var addr = address.ToBigEndianHex();
            var old = _sqldb.Addresses.FirstOrDefault(a => a.Address == addr);
            if (old == null)
            {
                old = new AddressEntity() { Hash = address.ToArray(), Address = addr };
                _sqldb.Addresses.Add(old);
                _sqldb.SaveChanges();
            }
            _addressCache[address] = old;
            return old;
        }

        private readonly Dictionary<UInt160, AssetEntity> _assetCache = new Dictionary<UInt160, AssetEntity>();
        private AssetEntity GetOrCreateAsset(AssetInfo asset)
        {
            if (_assetCache.ContainsKey(asset.Asset))
            {
                return _assetCache[asset.Asset];
            }
            var assetScriptHash = asset.Asset.ToBigEndianHex();
            var old = _sqldb.Assets.FirstOrDefault(a => a.Asset == assetScriptHash);
            if (old == null)
            {
                old = new AssetEntity() { Hash = asset.Asset.ToArray(), Asset = assetScriptHash, Name = asset.Name, Symbol = asset.Symbol, Decimals = asset.Decimals, TotalSupply = asset.TotalSupply.ToByteArray() };
                _sqldb.Assets.Add(old);
                _sqldb.SaveChanges();
            }
            _assetCache[asset.Asset] = old;
            return old;
        }

        private AssetBalanceEntity GetOrCreateBalance(AddressEntity address, AssetEntity asset, BigInteger balance, uint height)
        {
            var old = _sqldb.AssetBalances.FirstOrDefault(a => a.AddressId == address.Id && a.AssetId == asset.Id);
            if (old == null)
            {
                old = new AssetBalanceEntity() { AddressId = address.Id, AssetId = asset.Id, Balance = balance.ToByteArray(), BlockHeight = height };
                _sqldb.AssetBalances.Add(old);
                _sqldb.SaveChanges();
            }
            return old;
        }


        private IQueryable<Nep5TransactionEntity> BuildQuery(TransferFilter filter)
        {
            IQueryable<Nep5TransactionEntity> query = _sqldb.Nep5Transactions
                .Include(t => t.From)
                .Include(t => t.To)
                .Include(t => t.Asset);

            if (filter.FromOrTo.NotEmpty())
            {
                var addresses = filter.FromOrTo.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.From.Address) || addresses.Contains(r.To.Address));
            }
            if (filter.From.NotEmpty())
            {
                var addresses = filter.From.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.From.Address));
            }
            if (filter.To.NotEmpty())
            {
                var addresses = filter.To.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.To.Address));
            }
            if (filter.StartTime != null)
            {
                query = query.Where(r => r.Time >= filter.StartTime.Value.ToUniversalTime());
            }
            if (filter.EndTime != null)
            {
                query = query.Where(r => r.Time <= filter.EndTime.Value.ToUniversalTime());
            }
            if (filter.Asset != null)
            {
                query = query.Where(r => r.Asset.Asset == filter.Asset.ToBigEndianHex());
            }
            if (filter.BlockHeight != null)
            {
                query = query.Where(r => r.BlockHeight == filter.BlockHeight);
            }
            if (filter.TxIds.NotEmpty())
            {
                var txids = filter.TxIds.Select(t => t.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(r => txids.Contains(r.TxId));
            }

            return query;
        }


        private TransferInfo ToNep5TransferInfo(Nep5TransactionEntity entity)
        {
            return new TransferInfo()
            {
                BlockHeight = entity.BlockHeight,
                TxId = UInt256.Parse(entity.TxId),
                From = entity.From != null ? UInt160.Parse(entity.From.Address) : null,
                To = UInt160.Parse(entity.To.Address),
                Amount = new BigInteger(entity.Amount),
                Asset = UInt160.Parse(entity.Asset.Asset),

                TimeStamp = entity.Time.AsUtcTime().ToTimestampMS(),
            };
        }

        #endregion




        public void Dispose()
        {
            _sqldb?.Dispose();
            _leveldb?.Dispose();
        }
    }
}
