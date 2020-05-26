using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Ledger;
using Neo.Models.Wallets;
using Neo.SmartContract.Native;

namespace Neo.Models.Jobs
{
    public class SyncWalletJob : Job
    {
        public SyncWalletJob(TimeSpan timeSpan)
        {
            IntervalTime = timeSpan;
        }
        public override async Task<WsMessage> Invoke()
        {
            if (Program.Starter.CurrentWallet != null)
            {
                var accounts = Program.Starter.CurrentWallet.GetAccounts().ToList();

                using var snapshot = Blockchain.Singleton.GetSnapshot();
                var neoBalances = accounts.Select(a => a.ScriptHash).GetBalanceOf(NativeContract.NEO.Hash, snapshot);
                var gasBalances = accounts.Select(a => a.ScriptHash).GetBalanceOf(NativeContract.GAS.Hash, snapshot);

                var model = accounts.Select((a, i) => new AccountModel
                {
                    ScriptHash = a.ScriptHash,
                    Address = a.Address,
                    WatchOnly = a.WatchOnly,
                    AccountType = a.GetAccountType(snapshot),
                    Neo = neoBalances[i].ToString(),
                    Gas = gasBalances[i].ToString(),
                }).ToList();

                return new WsMessage()
                {
                    MsgType = WsMessageType.Push,
                    Method = "getWalletBalance",
                    Result = model,
                };
            }

            return null;
        }
    }
}
