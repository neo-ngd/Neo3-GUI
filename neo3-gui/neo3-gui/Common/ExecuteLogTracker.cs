using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Common.Storage;
using Neo.Common.Storage.LevelDBModules;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Plugins;
using Neo.VM;

namespace Neo.Common
{
    public class ExecuteLogTracker : Plugin, IPersistencePlugin
    {
        private TrackDB _db = new TrackDB();

        public void OnPersist(StoreView snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            foreach (var appExec in applicationExecutedList.Where(e => e.Transaction != null))
            {
                var execResult = new ExecuteResultInfo();
                execResult.TxId = appExec.Transaction.Hash.ToString();
                execResult.Trigger = appExec.Trigger;
                execResult.VMState = appExec.VMState;
                execResult.GasConsumed = appExec.GasConsumed;
                try
                {
                    execResult.ResultStack = appExec.Stack.Select(q => q.ToParameter().ToJson()).ToArray();
                }
                catch (InvalidOperationException)
                {
                    execResult.ResultStack = "error: recursive reference";
                }
                execResult.Notifications = appExec.Notifications.Select(q =>
                 {
                     var notification = new NotificationInfo();
                     notification.Contract = q.ScriptHash.ToString();
                     try
                     {
                         notification.State = q.State.ToParameter().ToJson();
                     }
                     catch (InvalidOperationException)
                     {
                         notification.State = "error: recursive reference";
                     }
                     return notification;
                 }).ToList();
                _db.AddExecuteLog(appExec.Transaction.Hash, execResult);
            }
        }

        public void OnCommit(StoreView snapshot)
        {
            _db.Commit();
            if (_db.LiveTime.TotalSeconds > 15)
            {
                //release memory
                _db.Dispose();
                _db = new TrackDB();
            }
        }

        public bool ShouldThrowExceptionFromCommit(Exception ex)
        {
            return false;
        }
    }
}
