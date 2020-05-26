using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Akka.Util;
using Neo.Common.Storage;
using Neo.Common.Storage.LevelDBModules;
using Neo.Common.Utility;
using Neo.Ledger;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Newtonsoft.Json.Bson;

namespace Neo.Common
{
    /// <summary>
    /// Scan execute result log background
    /// </summary>
    public class ExecuteResultScanner
    {
        private TrackDB _db = new TrackDB();
        private bool _running = true;
        private uint _scanHeight = 0;
        public async Task Start()
        {
            _running = true;
            _scanHeight = _db.GetMaxSyncIndex() ?? 0;
            while (_running)
            {
                try
                {
                    if (await Sync(_scanHeight))
                    {
                        _scanHeight++;
                    }
                    if (_scanHeight > Blockchain.Singleton.Height)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }

        public void Stop()
        {
            _running = false;
        }



        /// <summary>
        /// analysis block transaction execute result logs
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public async Task<bool> Sync(uint blockHeight)
        {
            if (blockHeight > Blockchain.Singleton.Height)
            {
                return false;
            }

            if (_db.HasSyncIndex(blockHeight))
            {
                return true;
            }

            var block = Blockchain.Singleton.GetBlock(blockHeight);
            if (block.Transactions.IsEmpty())
            {
                return true;
            }
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            foreach (var transaction in block.Transactions)
            {
                _db.AddTransaction(new TransactionInfo()
                {
                    TxId = transaction.Hash,
                    BlockHeight = blockHeight,
                    Sender = transaction.Sender,
                    Time = block.Timestamp.FromTimestampMS(),
                });
                var executeResult = _db.GetExecuteLog(transaction.Hash);
                if (executeResult == null || executeResult.VMState.HasFlag(VMState.FAULT) || executeResult.Notifications.IsEmpty())
                {
                    continue;
                }

                foreach (var notification in executeResult.Notifications)
                {
                    HasTransfer(notification, transaction, block, snapshot);
                }

                //shouldCommit = executeResult.Notifications.Aggregate(shouldCommit, (current, notification) => current | HasTransfer(notification, transaction, block, snapshot));
            }

            _db.AddSyncIndex(blockHeight);
            _db.Commit();
            Console.WriteLine($"Syncing:{_scanHeight}");
            if (_db.LiveTime.TotalSeconds > 15)
            {
                //release memory
                _db.Dispose();
                _db = new TrackDB();
            }
            return true;
        }



        /// <summary>
        /// try to find "Transfer" event, then add record to db
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="transaction"></param>
        /// <param name="block"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        private bool HasTransfer(NotificationInfo notification, Transaction transaction, Block block, SnapshotView snapshot)
        {
            var assetHash = UInt160.Parse(notification.Contract);
            var asset = AssetCache.GetAssetInfo(assetHash, snapshot);
            if (asset == null)
            {
                //not nep5 asset
                return false;
            }
            var notify = JStackItem.FromJson(notification.State);
            if (!(notify.Value is IList<JStackItem> notifyArray) || notifyArray.Count < 4)
            {
                return false;
            }
            if (!"transfer".Equals(notifyArray[0].ValueString, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var from = notifyArray[1].Value as byte[];
            var to = notifyArray[2].Value as byte[];
            if (from == null && to == null)
            {
                return false;
            }
            if (!ConvertBigInteger(notifyArray[3], out var amount))
            {
                return false;
            }

            var record = new TransferInfo
            {
                BlockHeight = block.Index,
                From = from == null ? null : new UInt160(from),
                To = to == null ? null : new UInt160(to),
                Asset = asset.Asset,
                Amount = amount,
                TxId = transaction.Hash,
                TimeStamp = block.Timestamp,
                AssetInfo = asset,
            };
            _db.AddTransfer(record);

            if (record.From != null)
            {
                var fromBalance = record.From.GetBalanceOf(assetHash, snapshot);
                _db.UpdateBalance(record.From, asset, fromBalance.Value, snapshot.Height);
            }

            if (record.To != null && record.To != record.From)
            {
                var toBalance = record.To.GetBalanceOf(assetHash, snapshot);
                _db.UpdateBalance(record.To, asset, toBalance.Value, snapshot.Height);
            }
            return true;
        }


        private bool ConvertBigInteger(JStackItem item, out BigInteger amount)
        {
            amount = 0;
            if (item.TypeCode == ContractParameterType.Integer)
            {
                amount = (BigInteger)item.Value;
                return true;
            }
            if (item.TypeCode == ContractParameterType.ByteArray)
            {
                amount = new BigInteger((byte[])item.Value);
                return true;
            }
            return false;
        }

    }
}
