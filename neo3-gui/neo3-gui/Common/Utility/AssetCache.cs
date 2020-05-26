using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Neo.Common.Storage;
using Neo.Common.Storage.LevelDBModules;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Common.Utility
{
    public class AssetCache
    {

        private static readonly ConcurrentDictionary<UInt160, AssetInfo> _assets = new ConcurrentDictionary<UInt160, AssetInfo>();


        /// <summary>
        /// read nep5 from cache first
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static AssetInfo GetAssetInfo(UInt160 assetId, bool readFromDb = true)
        {
            if (_assets.ContainsKey(assetId))
            {
                return _assets[assetId];
            }
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            var asset = GetAssetInfoFromChain(assetId, snapshot) ?? (readFromDb ? GetAssetInfoFromDb(assetId) : null);
            return asset;
        }


        /// <summary>
        /// read nep5 from cache first
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static AssetInfo GetAssetInfo(UInt160 assetId, StoreView snapshot)
        {
            if (_assets.ContainsKey(assetId))
            {
                return _assets[assetId];
            }
            return GetAssetInfoFromChain(assetId, snapshot);
        }


        /// <summary>
        /// read nep5 from chain, and set cache
        /// https://github.com/neo-project/proposals/blob/master/nep-5.mediawiki
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static AssetInfo GetAssetInfoFromChain(UInt160 assetId, StoreView snapshot)
        {
            using var sb = new ScriptBuilder();
            sb.EmitAppCall(assetId, "decimals");
            sb.EmitAppCall(assetId, "symbol");
            sb.EmitAppCall(assetId, "name");


            var contract = snapshot.Contracts.TryGet(assetId);
            if (contract == null)
            {
                return null;
            }

            try
            {
                using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
                string name = engine.ResultStack.Pop().GetString();
                string symbol = engine.ResultStack.Pop().GetString();
                byte decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
                symbol = symbol == "neo" || symbol == "gas" ? symbol.ToUpper() : symbol;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }
                var assetInfo = new AssetInfo()
                {
                    Asset = assetId,
                    Decimals = decimals,
                    Symbol = symbol,
                    Name = name,
                };
                _assets[assetId] = assetInfo;
                return assetInfo;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Invalid Nep5[{assetId}]:{e}");
                return null;
            }
        }





        public static AssetInfo GetAssetInfoFromDb(UInt160 assetId)
        {
            using var db = new LevelDbContext();
            var oldAsset = db.GetAssetInfo(assetId);
            if (oldAsset != null)
            {
                var asset = new AssetInfo()
                {
                    Asset = assetId,
                    Decimals = oldAsset.Decimals,
                    Name = oldAsset.Name,
                    Symbol = oldAsset.Symbol,
                    //TotalSupply = oldAsset.TotalSupply,
                };
                _assets[assetId] = asset;
                return asset;
            }
            return null;
        }


        public static BigInteger? GetTotalSupply(UInt160 asset)
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            using var sb = new ScriptBuilder();
            sb.EmitAppCall(asset, "totalSupply");
            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
            var total = engine.ResultStack.FirstOrDefault();
            return ToTotalSupply(total);
        }

        public static List<BigInteger?> GetTotalSupply(IEnumerable<UInt160> assets)
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            using var sb = new ScriptBuilder();
            foreach (var asset in assets)
            {
                sb.EmitAppCall(asset, "totalSupply");
            }
            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
            return engine.ResultStack.Select(ToTotalSupply).ToList();
        }

        private static BigInteger? ToTotalSupply(StackItem value) => value is Null ? (BigInteger?)null : value.GetBigInteger();
    }
}
