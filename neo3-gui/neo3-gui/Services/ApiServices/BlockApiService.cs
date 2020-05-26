using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Akka.IO;
using Neo.Common;
using Neo.Common.Storage;
using Neo.Common.Utility;
using Neo.Ledger;
using Neo.Models;
using Neo.Models.Blocks;
using Neo.Models.Wallets;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Services.ApiServices
{
    public class BlockApiService : ApiService
    {
        /// <summary>
        /// get block by height
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task<object> GetBlock(uint index)
        {
            var block = Blockchain.Singleton.GetBlock(index);
            if (block == null)
            {
                return Error(ErrorCode.BlockHeightInvalid);
            }
            return ToBlockModel(block);
        }

        /// <summary>
        /// get block by hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public async Task<object> GetBlockByHash(UInt256 hash)
        {
            var block = Blockchain.Singleton.GetBlock(hash);
            if (block == null)
            {
                return Error(ErrorCode.BlockHashInvalid);
            }

            return ToBlockModel(block);
        }



        /// <summary>
        /// get latest blocks info
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<object> GetLastBlocks(int limit = 10, int? height = null)
        {
            var lastHeight = Blockchain.Singleton.Height;
            if (height > lastHeight)
            {
                return Error(ErrorCode.BlockHeightInvalid);
            }

            height ??= (int)lastHeight;
            var blocks = await GetBlockByRange((height.Value - limit + 1), (int)height);

            var result = blocks.Select(b => new BlockPreviewModel(b));
            return result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<object> GetAllAssets()
        {
            using var db = new TrackDB();
            var result = db.GetAllContracts()?.Where(a => a.Symbol.NotNull() && a.DeleteOrMigrateTxId == null).Select(a =>
                 new AssetInfoModel()
                 {
                     Asset = a.Hash,
                     Decimals = a.Decimals,
                     Name = a.Name,
                     Symbol = a.Symbol,
                     CreateTime = a.CreateTime,
                 }).ToList();


            var totalSupplies = AssetCache.GetTotalSupply(result.Select(r => r.Asset));
            for (var i = 0; i < result.Count; i++)
            {
                result[i].TotalSupply = totalSupplies[i];
            }
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<object> GetAsset(UInt160 asset)
        {
            var assetInfo = AssetCache.GetAssetInfo(asset);
            if (assetInfo == null)
            {
                return null;
            }
            var totalSupply = AssetCache.GetTotalSupply(asset);
            return new AssetInfoModel()
            {
                Asset = assetInfo.Asset,
                Decimals = assetInfo.Decimals,
                Name = assetInfo.Name,
                Symbol = assetInfo.Symbol,
                TotalSupply = totalSupply,
                //CreateTime = a.CreateTime,
            };
        }


        public async Task<object> GetAddressBalance(UInt160[] addresses, UInt160[] assets)
        {
            using var db = new TrackDB();
            var balances = db.FindAssetBalance(new BalanceFilter()
            {
                Addresses = addresses,
                Assets = assets,
            });
            return balances.ToLookup(b => b.Address).ToAddressBalanceModels();
        }


        public async Task<object> GetSync()
        {
            using var db = new TrackDB();
            return db.GetMaxSyncIndex();
        }

        #region Private

        private BlockModel ToBlockModel(Block block)
        {
            var model = new BlockModel(block);
            model.Confirmations = Blockchain.Singleton.Height - block.Index + 1;

            //if (block.Transactions.NotEmpty())
            //{
            //    using var db = new TrackDB();
            //    var trans = db.FindTransfer(new TrackFilter() { TxIds = block.Transactions.Select(t => t.Hash).ToList() });
            //    model.Transactions = trans.List.ToTransactionPreviewModel();
            //}

            return model;
        }

        private Block GetBlockByHeight(uint height)
        {
            var block = Blockchain.Singleton.GetBlock(height);
            return block;
        }

        private async Task<IEnumerable<Block>> GetBlockByRange(int low, int high)
        {
            low = low < 0 ? 0 : low;
            high = high > Blockchain.Singleton.Height ? (int)Blockchain.Singleton.Height : high;

            var tasks = Enumerable.Range(low, high - low + 1).Reverse().Select(async (i) => GetBlockByHeight((uint)i));
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result);
        }
        #endregion
    }
}
