using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo.Common.Storage;
using Neo.Common.Utility;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.SmartContract.Native;

namespace Neo.Common
{

    public class ConsensusScanner
    {
        private static readonly Dictionary<ECPoint, UInt160> _consensusMap = new Dictionary<ECPoint, UInt160>();

        private static bool _running = false;
        private static Task _task = null;
        public static void StartLoop()
        {
            if (_task != null)
            {
                return;
            }
            _running = true;
            _task = Task.Run(Loop);
        }




        public static async Task StopLoop()
        {
            _running = false;
            _task = null;
        }


        private static async Task Loop()
        {
            while (_running)
            {
                UpdateConsensusBalance();
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private static void UpdateConsensusBalance()
        {
            if (Blockchain.Singleton.Height > 0)
            {
                using var snapshot = Blockchain.Singleton.GetSnapshot();
                var validators = NativeContract.NEO.GetValidators(snapshot);
                var addresses = new List<UInt160>();
                foreach (var ecPoint in validators)
                {
                    if (!_consensusMap.ContainsKey(ecPoint))
                    {
                        _consensusMap[ecPoint] = ecPoint.ToVerificationContract().ScriptHash;
                    }

                    addresses.Add(_consensusMap[ecPoint]);
                }
                using var db = new TrackDB();
                //var neobalances = addresses.GetBalanceOf(NativeContract.NEO.Hash, snapshot);
                var balances = addresses.GetBalanceOf(NativeContract.GAS.Hash, snapshot);

                for (var index = 0; index < addresses.Count; index++)
                {
                    var address = addresses[index];
                    //db.UpdateBalance(address, NativeContract.NEO.Hash, neobalances[index].Value, snapshot.Height);
                    db.UpdateBalance(address, NativeContract.GAS.Hash, balances[index].Value, snapshot.Height);
                }

                db.Commit();
            }
        }


    }

}