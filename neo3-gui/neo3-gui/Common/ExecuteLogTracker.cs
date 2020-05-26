using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Akka.Event;
using Neo.Common.Storage;
using Neo.Common.Storage.LevelDBModules;
using Neo.Common.Utility;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.Common
{

    public class ExecuteLogTracker : Plugin, IPersistencePlugin
    {
        private readonly LevelDbContext _levelDb = new LevelDbContext();

        public void OnPersist(StoreView snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            Header header = snapshot.GetHeader(snapshot.CurrentBlockHash);

            var analyzer = new BlockAnalyzer(snapshot, header, applicationExecutedList);
            analyzer.Analysis();

            foreach (var analyzerResultInfo in analyzer.AnalysisResult.ExecuteResultInfos)
            {
                _levelDb.SaveExecuteLog(analyzerResultInfo);
            }
            foreach (var analyzerAssetInfo in analyzer.AnalysisResult.AssetInfos)
            {
                _levelDb.SaveAssetInfo(analyzerAssetInfo.Value);
            }

            foreach (var transaction in analyzer.AnalysisResult.Transfers.Where(t => t.Value.NotEmpty()))
            {
                _levelDb.SaveTransfers(transaction.Key, transaction.Value);
            }

            foreach (var item in analyzer.AnalysisResult.BalanceChangeAccounts)
            {
                var balance = item.account.GetBalanceOf(item.asset, snapshot);
                _levelDb.UpdateBalance(item.account, item.asset, balance.Value, snapshot.Height);
            }


            if (analyzer.AnalysisResult.ContractEvents.NotEmpty())
            {
                _levelDb.SaveContractEvent(snapshot.Height, analyzer.AnalysisResult.ContractEvents);
            }
        }

        public void OnCommit(StoreView snapshot)
        {
            _levelDb.Commit();
        }

        public bool ShouldThrowExceptionFromCommit(Exception ex)
        {
            return false;
        }
    }
}
