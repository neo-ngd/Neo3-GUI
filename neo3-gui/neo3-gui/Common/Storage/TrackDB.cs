using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
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

        static TrackDB()
        {
            if (!Directory.Exists("Data_Track"))
            {
                Directory.CreateDirectory("Data_Track");
            }
        }

        public TrackDB()
        {
            _magic = ProtocolSettings.Default.Magic;
            _sqldb = new SQLiteContext(Path.Combine($"Data_Track", $"track.{_magic}.db"));
            _leveldb = new LevelDbContext(Path.Combine("Data_Track", $"TransactionLog_LevelDB_{_magic}"));

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
            //throw new Exception("track db damaged!");
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
        }


        public uint? GetMaxSyncIndex()
        {
            return _leveldb.GetMaxSyncIndex(_sqldb.Identity);
        }

        public void SetMaxSyncIndex(uint index)
        {
            _leveldb.SetMaxSyncIndex(_sqldb.Identity, index);
        }

        #endregion


        #region ExecuteResult

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
            var asset = GetAsset(newTransaction.Asset);

            var tran = new Nep5TransferEntity
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
        /// <param name="assetHash"></param>
        /// <param name="balance"></param>
        /// <param name="height"></param>
        public void UpdateBalance(UInt160 addressHash, UInt160 assetHash, BigInteger balance, uint height)
        {
            if (addressHash == null || assetHash == null) return;
            var asset = GetAsset(assetHash);
            if (asset == null)
            {
                throw new Exception($"Unkown asset:{assetHash}");
            }
            var address = GetOrCreateAddress(addressHash);
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
        /// update record will save after call <see cref="Commit"/> method;
        /// new record will save immediately
        /// </summary>
        /// <param name="addressHash"></param>
        /// <param name="assetInfo"></param>
        /// <param name="balance"></param>
        /// <param name="height"></param>
        public void UpdateBalanceIndex(UInt160 addressHash, AssetInfo assetInfo, BigInteger balance, uint height)
        {
            if (addressHash == null) return;
            var address = GetOrCreateAddress(addressHash);
            var asset = GetAsset(assetInfo.Asset);
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
        public PageList<TransactionInfo> QueryTransactions(TransactionFilter filter, bool includeTransfers = false)
        {
            IQueryable<TransactionEntity> query = _sqldb.Transactions;
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
            if (filter.FromOrTo.NotEmpty())
            {
                var addresses = filter.FromOrTo.Select(a => a.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(tx =>
                    tx.Transfers.Any(t => addresses.Contains(t.From.Hash) || addresses.Contains(t.To.Hash)));
            }
            if (filter.From.NotEmpty())
            {
                var addresses = filter.From.Select(a => a.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(tx =>
                    tx.Transfers.Any(t => addresses.Contains(t.From.Hash)));
            }
            if (filter.To.NotEmpty())
            {
                var addresses = filter.To.Select(a => a.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(tx =>
                    tx.Transfers.Any(t => addresses.Contains(t.To.Hash)));
            }

            if (filter.Contracts.NotEmpty())
            {
                var contracts = filter.Contracts.Select(a => a.ToBigEndianHex()).Distinct().ToList();
                query = query.Where(tx => tx.InvokeContracts.Any(c => contracts.Contains(c.Contract.Hash)));

                //query = query.Join(_sqldb.InvokeRecords,tx=>tx.TxId,r=>r.TxId,(tx,r)=>new{tx,r}).Where(tx => tx.Any(c => contracts.Contains(c.Contract.Hash)));

            }
            var pageList = new PageList<TransactionInfo>();
            var pageIndex = filter.PageIndex <= 0 ? 0 : filter.PageIndex - 1;
            pageList.TotalCount = query.Count();
            pageList.PageIndex = pageIndex + 1;
            pageList.PageSize = filter.PageSize;
            if (filter.PageSize > 0)
            {
                var list = query.OrderByDescending(g => g.Time)
                    .Skip(pageIndex * filter.PageSize)
                    .Take(filter.PageSize);
                pageList.List.AddRange(includeTransfers ? list.Select(ToTransactionWithTransfer) : list.Select(ToTransactionWithoutTransfer));
            }
            return pageList;
        }


        /// <summary>
        /// query without transfers(High Performance)
        /// </summary>
        private readonly Expression<Func<TransactionEntity, TransactionInfo>> ToTransactionWithoutTransfer = (tx) => new TransactionInfo()
        {
            TxId = UInt256.Parse(tx.TxId),
            BlockHeight = tx.BlockHeight,
            Sender = tx.Sender != null ? UInt160.Parse(tx.Sender.Hash) : null,
            Time = tx.Time.AsUtcTime(),
        };

        /// <summary>
        ///  query with transfers(Low Performance)
        /// </summary>
        private readonly Expression<Func<TransactionEntity, TransactionInfo>> ToTransactionWithTransfer = (tx) => new TransactionInfo()
        {
            TxId = UInt256.Parse(tx.TxId),
            BlockHeight = tx.BlockHeight,
            Sender = tx.Sender != null ? UInt160.Parse(tx.Sender.Hash) : null,
            Time = tx.Time.AsUtcTime(),
            Transfers = tx.Transfers.Select(t => new TransferInfo()
            {
                From = t.From != null ? UInt160.Parse(t.From.Hash) : null,
                To = t.To != null ? UInt160.Parse(t.To.Hash) : null,
                Amount = new BigInteger(t.Amount),
                TxId = UInt256.Parse(t.TxId),
                Asset = UInt160.Parse(t.Asset.Hash),
                TimeStamp = t.Time.ToTimestampMS(),
            }).ToList()
        };



        /// <summary>
        ///  Paged by Transactions
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public PageList<TransferInfo> QueryNep5Transactions(TransferFilter filter)
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
        public PageList<TransferInfo> QueryTransfers(TransferFilter filter)
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
                query = query.Where(q => addrs.Contains(q.Address.Hash));
            }

            if (filter.Assets.NotEmpty())
            {
                var assets = filter.Assets.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(q => assets.Contains(q.Asset.Hash));
            }

            var balances = query.ToList();
            return balances.Select(b => new BalanceInfo()
            {
                Address = UInt160.Parse(b.Address.Hash),
                Asset = UInt160.Parse(b.Asset.Hash),
                AssetName = b.Asset.Name,
                AssetSymbol = b.Asset.Symbol,
                AssetDecimals = b.Asset.Decimals,
                Balance = new BigInteger(b.Balance),
                BlockHeight = b.BlockHeight,
            });
        }


        public IEnumerable<Nep5ContractInfo> GetAllContracts()
        {
            return _sqldb.Contracts.Select(c => new Nep5ContractInfo()
            {
                Hash = UInt160.Parse(c.Hash),
                Name = c.Name,
                Symbol = c.Symbol,
                Decimals = c.Decimals,
                CreateTime = c.CreateTime,
                CreateTxId = c.CreateTxId != null ? UInt256.Parse(c.CreateTxId) : null,
                DeleteOrMigrateTxId = c.DeleteOrMigrateTxId != null ? UInt256.Parse(c.DeleteOrMigrateTxId) : null,
                DeleteTime = c.DeleteTime,
                MigrateTime = c.MigrateTime,
            }).ToList();
        }

        #endregion
        

        #region Transaction

        public void AddTransaction(TransactionInfo transaction)
        {
            var txId = transaction.TxId.ToBigEndianHex();
            var old = _sqldb.Transactions.FirstOrDefault(t => t.TxId == txId);
            if (old != null)
            {
                return;
            }
            var sender = GetOrCreateAddress(transaction.Sender);
            _sqldb.Transactions.Add(new TransactionEntity()
            {
                TxId = txId,
                BlockHeight = transaction.BlockHeight,
                Time = transaction.Time,
                SenderId = sender?.Id,
            });
        }


        public void AddInvokeTransaction(UInt256 txId, UInt160 contract, string method)
        {
            var contractHash = contract.ToBigEndianHex();
            var contractEntity = _sqldb.Contracts.FirstOrDefault(c => c.Hash == contractHash);
            if (contractEntity != null)
            {
                _sqldb.InvokeRecords.Add(new InvokeRecordEntity()
                {
                    ContractId = contractEntity.Id,
                    TxId = txId.ToBigEndianHex(),
                    Methods = method,
                });
            }
        }
        #endregion


        #region Contract



        /// <summary>
        /// Create contract, save immediately
        /// </summary>
        /// <param name="newContract"></param>
        public void CreateContract(Nep5ContractInfo newContract)
        {
            var contractHash = newContract.Hash.ToBigEndianHex();
            var old = _sqldb.Contracts.FirstOrDefault(c => c.Hash == contractHash);
            if (old == null)
            {
                var contract = new ContractEntity()
                {
                    Hash = contractHash,
                    Name = newContract.Name,
                    Symbol = newContract.Symbol,
                    Decimals = newContract.Decimals,
                    CreateTime = newContract.CreateTime,
                    CreateTxId = newContract.CreateTxId?.ToBigEndianHex(),
                };
                _sqldb.Contracts.Add(contract);
                _sqldb.SaveChanges();

                //save create contract transaction record
                _sqldb.InvokeRecords.Add(new InvokeRecordEntity()
                {
                    ContractId = contract.Id,
                    TxId = contract.CreateTxId,
                });
                _sqldb.SaveChanges();
            }
        }


        /// <summary>
        /// Destroy contract, save immediately
        /// </summary>
        /// <param name="contractHash"></param>
        /// <param name="txId"></param>
        /// <param name="time"></param>
        public void DeleteContract(UInt160 contractHash, UInt256 txId, DateTime time)
        {
            var contract = contractHash.ToBigEndianHex();
            var old = _sqldb.Contracts.FirstOrDefault(c => c.Hash == contract);
            if (old != null)
            {
                old.DeleteOrMigrateTxId = txId.ToBigEndianHex();
                old.DeleteTime = time;
                _sqldb.SaveChanges();
            }
        }

        /// <summary>
        /// Migrate contract, save immediately
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="migrateContract"></param>
        /// <param name="txId"></param>
        /// <param name="time"></param>
        public void MigrateContract(UInt160 contract, Nep5ContractInfo migrateContract, UInt256 txId, DateTime time)
        {
            var contractHash = contract.ToBigEndianHex();
            var migrateContractHash = migrateContract.Hash.ToBigEndianHex();
            var tx = txId.ToBigEndianHex();
            var old = _sqldb.Contracts.FirstOrDefault(c => c.Hash == contractHash);
            if (old != null)
            {
                old.DeleteOrMigrateTxId = tx;
                old.MigrateTo = migrateContractHash;
                old.MigrateTime = time;
                var newContract = new ContractEntity()
                {
                    Hash = migrateContractHash,
                    Name = migrateContract.Name,
                    Symbol = migrateContract.Symbol,
                    Decimals = migrateContract.Decimals,
                    CreateTime = migrateContract.CreateTime,
                    CreateTxId = tx,
                };
                _sqldb.Contracts.Add(newContract);
                _sqldb.SaveChanges();

                _sqldb.InvokeRecords.Add(new InvokeRecordEntity()
                {
                    ContractId = newContract.Id,
                    TxId = tx,
                });
                _sqldb.SaveChanges();
                // migrate balance records to new contract
                _sqldb.AssetBalances.Where(a => a.AssetId == old.Id).BatchUpdate(new AssetBalanceEntity() { AssetId = newContract.Id });
            }
        }


        #endregion

        #region Private


        private readonly ConcurrentDictionary<UInt160, AddressEntity> _addressCache = new ConcurrentDictionary<UInt160, AddressEntity>();
        private AddressEntity GetOrCreateAddress(UInt160 address)
        {
            if (address == null) return null;
            if (_addressCache.ContainsKey(address))
            {
                return _addressCache[address];
            }
            var addr = address.ToBigEndianHex();
            var old = _sqldb.Addresses.FirstOrDefault(a => a.Hash == addr);
            if (old == null)
            {
                old = new AddressEntity() { Hash = addr };
                _sqldb.Addresses.Add(old);
                _sqldb.SaveChanges();
            }
            _addressCache[address] = old;
            return old;
        }

        private readonly ConcurrentDictionary<UInt160, ContractEntity> _assetCache = new ConcurrentDictionary<UInt160, ContractEntity>();
        private ContractEntity GetAsset(UInt160 asset)
        {
            if (_assetCache.ContainsKey(asset))
            {
                return _assetCache[asset];
            }
            var assetScriptHash = asset.ToBigEndianHex();
            var old = _sqldb.Contracts.FirstOrDefault(a => a.Hash == assetScriptHash);
            if (old != null)
            {
                _assetCache[asset] = old;
            }
            return old;
        }

        private AssetBalanceEntity GetOrCreateBalance(AddressEntity address, ContractEntity asset, BigInteger balance, uint height)
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


        private IQueryable<Nep5TransferEntity> BuildQuery(TransferFilter filter)
        {
            IQueryable<Nep5TransferEntity> query = _sqldb.Nep5Transactions
                .Include(t => t.From)
                .Include(t => t.To)
                .Include(t => t.Asset);

            if (filter.FromOrTo.NotEmpty())
            {
                var addresses = filter.FromOrTo.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.From.Hash) || addresses.Contains(r.To.Hash));
            }
            if (filter.From.NotEmpty())
            {
                var addresses = filter.From.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.From.Hash));
            }
            if (filter.To.NotEmpty())
            {
                var addresses = filter.To.Select(a => a.ToBigEndianHex()).ToList();
                query = query.Where(r => addresses.Contains(r.To.Hash));
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
                query = query.Where(r => r.Asset.Hash == filter.Asset.ToBigEndianHex());
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


        private TransferInfo ToNep5TransferInfo(Nep5TransferEntity entity)
        {
            return new TransferInfo()
            {
                BlockHeight = entity.BlockHeight,
                TxId = UInt256.Parse(entity.TxId),
                From = entity.From != null ? UInt160.Parse(entity.From.Hash) : null,
                To = UInt160.Parse(entity.To.Hash),
                Amount = new BigInteger(entity.Amount),
                Asset = UInt160.Parse(entity.Asset.Hash),

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
