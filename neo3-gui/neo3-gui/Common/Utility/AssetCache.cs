using System.Collections.Generic;
using System.Linq;
using Neo.Ledger;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Common.Utility
{
    public class AssetCache
    {

        private static readonly Dictionary<UInt160, AssetInfo> _assets = new Dictionary<UInt160, AssetInfo>();


        /// <summary>
        /// https://github.com/neo-project/proposals/blob/master/nep-5.mediawiki
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static AssetInfo GetAssetInfoByCache(UInt160 assetId)
        {
            if (_assets.ContainsKey(assetId))
            {
                return _assets[assetId];
            }
            return null;
        }


        /// <summary>
        /// read nep5 from cache first
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static AssetInfo GetAssetInfo(UInt160 assetId)
        {
            if (_assets.ContainsKey(assetId))
            {
                return _assets[assetId];
            }
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            return GetAssetInfoFromChain(assetId, snapshot);
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
            sb.EmitAppCall(assetId, "totalSupply");
            sb.EmitAppCall(assetId, "decimals");
            sb.EmitAppCall(assetId, "symbol");
            sb.EmitAppCall(assetId, "name");


            var contract = snapshot.Contracts.TryGet(assetId);
            if (contract == null)
            {
                return null;
            }
            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
            var name = engine.ResultStack.Pop().GetString();
            var symbol = engine.ResultStack.Pop().GetString();
            var decimals = (byte)engine.ResultStack.Pop().GetBigInteger();
            var totalSupply = engine.ResultStack.Pop().GetBigInteger();

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
                TotalSupply = totalSupply,
            };
            _assets[assetId] = assetInfo;
            return assetInfo;
        }

        public static List<AssetInfo> GetAllAssets()
        {
            return _assets.Values.ToList();
        }
    }
}
