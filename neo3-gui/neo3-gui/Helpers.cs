using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Neo.Common;
using Neo.Common.Json;
using Neo.Common.Storage;
using Neo.Common.Utility;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Models;
using Neo.Models.Transactions;
using Neo.Models.Wallets;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Services;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using Neo.Wallets.SQLite;
using VmArray = Neo.VM.Types.Array;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Neo
{
    public static class Helpers
    {

        public static readonly JsonSerializerOptions SerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new UInt160Converter(),
                new UInt256Converter(),
                new NumToStringConverter(),
                new BigDecimalConverter(),
                new BigIntegerConverter(),
                new DatetimeJsonConverter(),
                new ByteArrayConverter(),
                new JObjectConverter(),
            }
        };



        public static string GetVersion(this Assembly assembly)
        {
            CustomAttributeData attribute = assembly.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(AssemblyInformationalVersionAttribute));
            if (attribute == null) return assembly.GetName().Version.ToString(3);
            return (string)attribute.ConstructorArguments[0].Value;
        }

        public static bool ToBool(this string input)
        {
            if (input == null) return false;

            input = input.ToLowerInvariant();

            return input == "true" || input == "yes" || input == "1";
        }

        public static bool IsYes(this string input)
        {
            if (input == null) return false;

            input = input.ToLowerInvariant();

            return input == "yes" || input == "y";
        }


        /// <summary>
        /// broadcast transaction and cache
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static async Task Broadcast(this Transaction tx)
        {
            Program.Starter.NeoSystem.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
            var task = Task.Run(() => UnconfirmedTransactionCache.AddTransaction(tx));
        }



        /// <summary>
        /// safe serialize signContext to avoid encoding issues
        /// </summary>
        /// <param name="signContext"></param>
        /// <returns></returns>
        public static string SafeSerialize(this ContractParametersContext signContext)
        {
            return signContext.ToJson().SerializeJson();
        }

        /// <summary>
        /// create tx by script and signer
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="script"></param>
        /// <param name="signers"></param>
        /// <returns></returns>
        public static Transaction InitTransaction(this Wallet wallet, byte[] script, params UInt160[] signers)
        {
            var cosigners = signers.Select(account => new Cosigner { Account = account }).ToArray();
            return InitTransaction(wallet, script, cosigners);
        }

        /// <summary>
        /// create tx by script and signer
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="script"></param>
        /// <param name="signers"></param>
        /// <returns></returns>
        public static Transaction InitTransaction(this Wallet wallet, byte[] script, params Cosigner[] signers)
        {
            var tx = wallet.MakeTransaction(script, null, null, signers);
            return tx;
        }


        /// <summary>
        /// append sign to signContext
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="signContext"></param>
        /// <returns></returns>
        public static bool SignContext(this Wallet wallet, ContractParametersContext signContext)
        {
            wallet.Sign(signContext);
            return signContext.Completed;
        }

        /// <summary>
        /// sign transaction
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        public static (bool, ContractParametersContext) TrySignTx(this Wallet wallet, Transaction tx)
        {
            var context = new ContractParametersContext(tx);
            var signResult = wallet.SignContext(context);
            if (signResult)
            {
                tx.Witnesses = context.GetWitnesses();
            }
            return (signResult, context);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task SendAsync(this WebSocket socket, object data)
        {
            if (data == null)
            {
                return;
            }

            var bytes = SerializeJsonBytes(data);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }


        /// <summary>
        /// serialize to utf8 json bytes, more performance than to json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeJsonBytes<T>(this T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, SerializeOptions);
        }

        /// <summary>
        /// serialize to utf8 json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeJson<T>(this T obj)
        {
            return JsonSerializer.Serialize(obj, SerializeOptions);
        }



        /// <summary>
        /// deserialize from json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this string json)
        {
            if (json.IsNull())
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(json, SerializeOptions);
        }



        /// <summary>
        /// deserialize from json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object DeserializeJson(this string json, Type targetType)
        {
            return JsonSerializer.Deserialize(json, targetType, SerializeOptions);
        }



        /// <summary>
        /// deserialize from json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonBytes"></param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this byte[] jsonBytes)
        {
            if (jsonBytes.IsEmpty())
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(jsonBytes, SerializeOptions);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public static void AddWebSocketInvoker(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketHub>();
            services.AddSingleton<WebSocketHubMiddleware>();
            services.AddSingleton<WebSocketSession>();
            services.AddSingleton<WebSocketExecutor>();
            var interfaceType = typeof(IApiService);
            var assembly = interfaceType.Assembly;
            foreach (var type in assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && interfaceType.IsAssignableFrom(t)))
            {
                services.AddSingleton(type);
            }
        }

        /// <summary>
        /// change to utc Time without change time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime AsUtcTime(this DateTime time)
        {
            return DateTime.SpecifyKind(time, DateTimeKind.Utc);
        }


        private static readonly Dictionary<Type, object> defaultValues = new Dictionary<Type, object>();

        /// <summary>
        /// get input type default value
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetDefaultValue(this Type type)
        {
            if (defaultValues.ContainsKey(type))
            {
                return defaultValues[type];
            }

            if (type.IsValueType)
            {
                var val = Activator.CreateInstance(type);
                defaultValues[type] = val;
                return val;
            }

            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static UInt160 ToUInt160(this byte[] bytes)
        {
            if (bytes?.Length == 20)
            {
                return new UInt160(bytes);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string TryToAddress(this byte[] bytes)
        {
            if (bytes?.Length == 20)
            {
                return new UInt160(bytes).ToAddress();
            }
            return null;
        }


        /// <summary>
        /// convert private key string(wif or hex) to bytes
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static byte[] ToPrivateKeyBytes(this string privateKey)
        {
            byte[] keyBytes;
            try
            {
                keyBytes = Wallet.GetPrivateKeyFromWIF(privateKey);
            }
            catch
            {
                keyBytes = privateKey.HexToBytes();
            }

            return keyBytes;
        }


        /// <summary>
        /// string is null or white space
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsNull(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }


        /// <summary>
        /// string is not null or white space
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool NotNull(this string text)
        {
            return !IsNull(text);
        }

        /// <summary>
        /// collection is null or empty
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }


        /// <summary>
        /// collection is not null or empty
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool NotEmpty<T>(this IEnumerable<T> source)
        {
            return !source.IsEmpty();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static AccountType GetAccountType(this WalletAccount account, SnapshotView snapshot)
        {
            if (account.Contract != null)
            {
                if (account.Contract.Script.IsMultiSigContract(out _, out int _))
                {
                    return AccountType.MultiSignature;
                }
                if (account.Contract.Script.IsSignatureContract())
                {
                    return AccountType.Standard;
                }
                if (snapshot.Contracts.TryGet(account.Contract.ScriptHash) != null)
                {
                    return AccountType.DeployedContract;
                }
            }
            return AccountType.NonStandard;
        }

        /// <summary>
        /// query neo/gas balance quickly
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="asset"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static List<BigDecimal> GetNativeBalanceOf<T>(this IEnumerable<UInt160> addresses, Nep5Token<T> asset, StoreView snapshot) where T : Nep5AccountState, new()
        {
            var balances = new List<BigDecimal>();
            foreach (var account in addresses)
            {
                var balance = asset.BalanceOf(snapshot, account);
                balances.Add(new BigDecimal(balance, asset.Decimals));
            }
            return balances;
        }

        /// <summary>
        /// query balance
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static List<BigDecimal> GetBalanceOf(this IEnumerable<UInt160> addresses, UInt160 assetId)
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            return GetBalanceOf(addresses, assetId, snapshot);
        }



        /// <summary>
        /// query balance
        /// </summary>
        /// <param name="addresses"></param>
        /// <param name="assetId"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static List<BigDecimal> GetBalanceOf(this IEnumerable<UInt160> addresses, UInt160 assetId, StoreView snapshot)
        {
            var assetInfo = AssetCache.GetAssetInfo(assetId, snapshot);
            if (assetInfo == null)
            {
                throw new ArgumentException($"invalid assetId:[{assetId}]");
            }

            if (assetInfo.Asset == NativeContract.NEO.Hash)
            {
                return GetNativeBalanceOf(addresses, NativeContract.NEO, snapshot);
            }
            if (assetInfo.Asset == NativeContract.GAS.Hash)
            {
                return GetNativeBalanceOf(addresses, NativeContract.GAS, snapshot);
            }
            using var sb = new ScriptBuilder();
            foreach (var address in addresses)
            {
                sb.EmitAppCall(assetId, "balanceOf", address);
            }

            using ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
            if (engine.State.HasFlag(VMState.FAULT))
            {
                throw new Exception($"query balance error");
            }

            var result = engine.ResultStack.Select(p => p.GetBigInteger());
            return result.Select(bigInt => new BigDecimal(bigInt, assetInfo.Decimals)).ToList();
        }

        /// <summary>
        /// query balance
        /// </summary>
        /// <param name="address"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static BigDecimal GetBalanceOf(this UInt160 address, UInt160 assetId)
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            return GetBalanceOf(address, assetId, snapshot);
        }

        /// <summary>
        /// query balance
        /// </summary>
        /// <param name="address"></param>
        /// <param name="assetId"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static BigDecimal GetBalanceOf(this UInt160 address, UInt160 assetId, StoreView snapshot)
        {
            var assetInfo = AssetCache.GetAssetInfo(assetId, snapshot);
            if (assetInfo == null)
            {
                return new BigDecimal(0, 0);
            }

            using var sb = new ScriptBuilder();
            sb.EmitAppCall(assetId, "balanceOf", address);
            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, testMode: true);
            if (engine.State.HasFlag(VMState.FAULT))
            {
                return new BigDecimal(0, 0);
            }
            var balances = engine.ResultStack.Pop().GetBigInteger();
            return new BigDecimal(balances, assetInfo.Decimals);
        }


        /// <summary>
        /// sum same asset amount
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static BigDecimal SumAssetAmount(this IEnumerable<BigDecimal> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException();
            }

            var item = source.FirstOrDefault();
            var total = source.Select(s => s.Value).Sum();
            return new BigDecimal(total, item.Decimals);
        }



        /// <summary>
        /// convert bigint to asset decimal value
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public static (BigDecimal amount, AssetInfo asset) GetAssetAmount(this BigInteger amount, UInt160 assetId)
        {
            var asset = AssetCache.GetAssetInfo(assetId);
            if (asset == null)
            {
                return (new BigDecimal(0, 0), null);
            }

            return (new BigDecimal(amount, asset.Decimals), asset);
        }

        /// <summary>
        /// convert to Big Endian hex string without "Ox"
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string ToBigEndianHex(this UInt160 address)
        {
            return address.ToArray().ToHexString(reverse: true);
        }

        /// <summary>
        /// convert to Big Endian hex string without "Ox"
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public static string ToBigEndianHex(this UInt256 txId)
        {
            return txId.ToArray().ToHexString(reverse: true);
        }

        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime FromTimestampMS(this ulong timestamp)
        {
            return unixEpoch.AddMilliseconds(timestamp);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
            return Expression.Lambda<Func<T, bool>>
                (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            if (expr1 == null)
            {
                return expr2;
            }

            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
            return Expression.Lambda<Func<T, bool>>
                (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }


        private static readonly ConcurrentDictionary<ErrorCode, string> _errorMap = new ConcurrentDictionary<ErrorCode, string>();

        public static WsError ToError(this ErrorCode code)
        {
            if (!_errorMap.ContainsKey(code))
            {
                var message = GetErrorMsg(code);
                _errorMap[code] = message ?? code.ToString();
            }

            return new WsError()
            {
                Code = (int)code,
                Message = _errorMap[code],
            };
        }

        private static string GetErrorMsg(this ErrorCode errorCode)
        {
            FieldInfo fieldInfo = errorCode.GetType().GetField(errorCode.ToString());
            var desc = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
            return desc?.Description;
        }

        public static bool NotVmByteArray(this StackItem item)
        {
            return !(item is ByteString);
        }

        public static bool NotVmNull(this StackItem item)
        {
            return !(item is Null);
        }


        public static bool NotVmInt(this StackItem item)
        {
            return !(item is Integer);
        }



        public static TransferNotifyItem GetTransferNotify(this VmArray notifyArray, UInt160 asset)
        {
            if (notifyArray.Count < 4) return null;
            // Event name should be encoded as a byte array.
            if (notifyArray[0].NotVmByteArray()) return null;

            var eventName = notifyArray[0].GetString();
            if (!"Transfer".Equals(eventName, StringComparison.OrdinalIgnoreCase)) return null;

            var fromItem = notifyArray[1];
            if (fromItem.NotVmByteArray() && fromItem.NotVmNull()) return null;

            var fromBytes = fromItem.GetByteSafely();
            if (fromBytes?.Length != 20)
            {
                fromBytes = null;
            }

            var toItem = notifyArray[2];
            if (toItem != null && toItem.NotVmByteArray()) return null;

            byte[] toBytes = toItem.GetByteSafely();
            if (toBytes?.Length != 20)
            {
                toBytes = null;
            }
            if (fromBytes == null && toBytes == null) return null;

            var amountItem = notifyArray[3];
            if (amountItem.NotVmByteArray() && amountItem.NotVmInt()) return null;

            var transfer = new TransferNotifyItem()
            {
                Asset = asset,
                From = fromBytes == null ? null : new UInt160(fromBytes),
                To = new UInt160(toBytes),
                Amount = amountItem.GetBigInteger(),
            };
            return transfer;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static TransactionPreviewModel ToTransactionPreviewModel(this UnconfirmedTransactionCache.TempTransaction transaction)
        {
            return new TransactionPreviewModel()
            {
                TxId = transaction.Tx.Hash,
                Transfers = transaction.Transfers?.Select(tran => tran.ToTransferModel()).ToList()
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        public static TransferModel ToTransferModel(this TransferNotifyItem transfer)
        {
            return new TransferModel()
            {
                From = transfer.From,
                To = transfer.To,
                Amount = new BigDecimal(transfer.Amount, transfer.Decimals).ToString(),
                Symbol = transfer.Symbol,
            };
        }


        public static byte[] GetByteSafely(this StackItem item)
        {
            try
            {
                switch (item)
                {
                    case Null _:
                        return null;
                }
                return item?.GetSpan().ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// try get private key from hex string or wif,error will return null
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static byte[] TryGetPrivateKey(this string privateKey)
        {
            if (privateKey.IsNull())
            {
                return null;
            }
            try
            {
                return Wallet.GetPrivateKeyFromWIF(privateKey);
            }
            catch (FormatException)
            {
            }
            if (privateKey.Length == 64)
            {
                try
                {
                    return privateKey.HexToBytes();
                }
                catch (Exception e)
                {
                }
            }
            return null;
        }

        /// <summary>
        /// convert to friendly transfer model
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        public static TransferModel ToTransferModel(this TransferInfo transfer)
        {
            var tran = new TransferModel()
            {
                From = transfer.From,
                To = transfer.To,
            };
            var (amount, asset) = transfer.Amount.GetAssetAmount(transfer.Asset);
            tran.Amount = amount.ToString();
            tran.Symbol = asset.Symbol;
            return tran;
        }

        /// <summary>
        /// convert to AddressBalanceModel list
        /// </summary>
        /// <param name="balances"></param>
        /// <returns></returns>
        public static List<AddressBalanceModel> ToAddressBalanceModels(this ILookup<UInt160, BalanceInfo> balances)
        {
            return balances.Select(b => new AddressBalanceModel()
            {
                AddressHash = b.Key,
                Balances = b.Select(assetBalance => new AssetBalanceModel()
                {
                    Asset = assetBalance.Asset,
                    Symbol = assetBalance.AssetSymbol,
                    Balance = new BigDecimal(assetBalance.Balance, assetBalance.AssetDecimals),
                }).ToList(),
            }).ToList();
        }


        public static BigDecimal ToNeo(this BigInteger amount)
        {
            return new BigDecimal(amount, NativeContract.NEO.Decimals);
        }

        public static BigDecimal ToGas(this BigInteger amount)
        {
            return new BigDecimal(amount, NativeContract.GAS.Decimals);
        }



        public static List<TransactionPreviewModel> ToTransactionPreviewModel(this IEnumerable<TransferInfo> trans)
        {
            return trans.ToLookup(x => x.TxId).Select(ToTransactionPreviewModel).ToList();
        }

        private static TransactionPreviewModel ToTransactionPreviewModel(IGrouping<UInt256, TransferInfo> lookup)
        {
            var item = lookup.FirstOrDefault();
            var model = new TransactionPreviewModel()
            {
                TxId = lookup.Key,
                Timestamp = item.TimeStamp,
                BlockHeight = item.BlockHeight,
                Transfers = lookup.Select(x => x.ToTransferModel()).ToList(),
            };
            return model;
        }


        public static VerificationContract ToVerificationContract(this ECPoint point)
        {
            VerificationContract contract = new VerificationContract
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(point),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            return contract;
        }


        public static bool ToBigInteger(this JStackItem item, out BigInteger amount)
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


        public static byte[] Append(this byte[] source, params byte[][] bytes)
        {
            IEnumerable<byte> data = source;
            foreach (var b in bytes)
            {
                data = data.Concat(b);
            }
            return data.ToArray();
        }



        public static NotificationInfo ToNotificationInfo(this NotifyEventArgs notify)
        {
            var notification = new NotificationInfo();
            notification.Contract = notify.ScriptHash.ToString();
            try
            {
                notification.State = notify.State.ToParameter().ToJson();
            }
            catch (InvalidOperationException)
            {
                notification.State = "error: recursive reference";
            }
            return notification;
        }

    }
}
