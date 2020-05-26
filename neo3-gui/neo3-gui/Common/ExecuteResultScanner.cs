using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Akka.Util;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Neo.Common.Storage;
using Neo.Common.Storage.LevelDBModules;
using Neo.Common.Utility;
using Neo.Ledger;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
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
        private LevelDbContext _levelDb = new LevelDbContext();
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
                _db.SetMaxSyncIndex(blockHeight);
                _db.Commit();
                return true;
            }

            var blockTime = block.Timestamp.FromTimestampMS();

            foreach (var transaction in block.Transactions)
            {
                _db.AddTransaction(new TransactionInfo()
                {
                    TxId = transaction.Hash,
                    BlockHeight = blockHeight,
                    Sender = transaction.Sender,
                    Time = blockTime,
                });
            }

            SyncContracts(blockHeight, blockTime);

            var transfers = new List<TransferInfo>();
            var balanceChanges = new HashSet<(UInt160 account, UInt160 asset)>();

            foreach (var transaction in block.Transactions)
            {
                var invokeMethods = GetInvokeMethods(transaction);
                if (invokeMethods.NotEmpty())
                {
                    foreach (var invokeMethod in invokeMethods)
                    {
                        _db.AddInvokeTransaction(transaction.Hash, invokeMethod.contract, string.Join(',', invokeMethod.methods));
                    }
                }
                var transferItems = _levelDb.GetTransfers(transaction.Hash);
                if (transferItems.NotEmpty())
                {
                    foreach (var item in transferItems)
                    {
                        transfers.Add(new TransferInfo()
                        {
                            BlockHeight = blockHeight,
                            TxId = transaction.Hash,
                            From = item.From,
                            To = item.To,
                            Amount = item.Amount,
                            TimeStamp = block.Timestamp,
                            Asset = item.Asset,
                        });
                        if (item.From != null)
                        {
                            balanceChanges.Add((item.From, item.Asset));
                        }
                        if (item.To != null)
                        {
                            balanceChanges.Add((item.To, item.Asset));
                        }
                    }
                }
            }

            foreach (var transferInfo in transfers)
            {
                _db.AddTransfer(transferInfo);
            }

            if (balanceChanges.NotEmpty())
            {
                using var snapshot = Blockchain.Singleton.GetSnapshot();
                foreach (var balanceChange in balanceChanges)
                {
                    UpdateBalance(balanceChange.account, balanceChange.asset, snapshot);
                }
            }

            _db.AddSyncIndex(blockHeight);
            _db.Commit();
            Console.WriteLine($"Synced:{_scanHeight}");
            if (_db.LiveTime.TotalSeconds > 15)
            {
                //release memory
                _db.Dispose();
                _db = new TrackDB();
            }
            return true;
        }


        /// <summary>
        /// sync contract create\update\delete state
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="blockTime"></param>
        private void SyncContracts(uint blockHeight, DateTime blockTime)
        {
            if (blockHeight == 0)
            {
                var tx = Blockchain.GenesisBlock.Transactions[0];
                // create native contract record
                foreach (NativeContract contract in NativeContract.Contracts)
                {
                    var newContract = new Nep5ContractInfo()
                    {
                        Hash = contract.Hash,
                        CreateTxId = tx.Hash,
                        CreateTime = blockTime,
                    };
                    var asset = AssetCache.GetAssetInfoFromDb(newContract.Hash);
                    if (asset != null)
                    {
                        newContract.Name = asset.Name;
                        newContract.Symbol = asset.Symbol;
                        newContract.Decimals = asset.Decimals;
                    }
                    _db.CreateContract(newContract);
                }
                return;
            }

            var contractEvents = _levelDb.GetContractEvent(blockHeight);
            if (contractEvents.NotEmpty())
            {
                foreach (var keyValuePair in contractEvents)
                {
                    var txId = keyValuePair.Key;
                    keyValuePair.Value?.ForEach(contractEvent => ProcessContractEvent(contractEvent, txId, blockTime));
                }
            }
        }

        /// <summary>
        /// save contract change info into sqldb immediately
        /// </summary>
        /// <param name="contractEvent"></param>
        /// <param name="txId"></param>
        /// <param name="blockTime"></param>
        private void ProcessContractEvent(ContractEventInfo contractEvent, UInt256 txId, DateTime blockTime)
        {
            switch (contractEvent.Event)
            {
                case ContractEventType.Create:
                    {
                        var newContract = GenerateNewNep5ContractInfo(contractEvent.Contract, txId, blockTime);
                        _db.CreateContract(newContract);
                        break;
                    }
                case ContractEventType.Destroy:
                    _db.DeleteContract(contractEvent.Contract, txId, blockTime);
                    break;
                case ContractEventType.Migrate:
                    var migrateContract = GenerateNewNep5ContractInfo(contractEvent.MigrateToContract, txId, blockTime);
                    _db.MigrateContract(contractEvent.Contract, migrateContract, txId, blockTime);
                    break;
            }
        }



        private Nep5ContractInfo GenerateNewNep5ContractInfo(UInt160 contract, UInt256 txId, DateTime blockTime)
        {
            var newContract = new Nep5ContractInfo()
            {
                Hash = contract,
                CreateTxId = txId,
                CreateTime = blockTime,
            };

            var asset = AssetCache.GetAssetInfo(contract);
            if (asset != null)
            {
                newContract.Name = asset.Name;
                newContract.Symbol = asset.Symbol;
                newContract.Decimals = asset.Decimals;
            }
            return newContract;
        }
        /// <summary>
        /// get invoke methods from transaction script
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private List<(UInt160 contract, HashSet<string> methods)> GetInvokeMethods(Transaction tx)
        {
            var methodBox = new Dictionary<UInt160, HashSet<string>>();

            var instructions = OpCodeConverter.Parse(tx.Script);
            for (int i = 2; i < instructions.Count; i++)
            {
                var currentInstruction = instructions[i];
                if (currentInstruction.OpCode == OpCode.SYSCALL && currentInstruction.SystemCallMethod == "System.Contract.Call")
                {
                    var contractInstruction = instructions[i - 1];
                    var contract = contractInstruction.OpData.ToUInt160();
                    if (contract == null) { continue; }

                    var methodInstruction = instructions[i - 2];
                    var method = methodInstruction.OpDataUtf8String;
                    if (method.NotNull())
                    {
                        var box = methodBox.ContainsKey(contract) ? methodBox[contract] : new HashSet<string>();
                        box.Add(method);
                        methodBox[contract] = box;
                    }
                }
            }
            return methodBox.Select(m => (m.Key, m.Value)).ToList();
        }



        private void UpdateBalance(UInt160 account, UInt160 asset, SnapshotView snapshot)
        {
            try
            {
                var balance = account.GetBalanceOf(asset, snapshot).Value;
                _db.UpdateBalance(account, asset, balance, snapshot.Height);
            }
            catch
            {
                var backupBalance = _levelDb.GetBalance(account, asset);
                if (backupBalance != null)
                {
                    _db.UpdateBalance(account, asset, backupBalance.Balance, backupBalance.Height);
                }
            }
        }
    }
}
