##  [Latest]

Compatible with Neo 3.9.1: [Neo v3.9.1](https://github.com/neo-project/neo/tree/v3.9.1)

### Updated

+ Upgraded to Neo 3.9.1 from local submodule
+ Upgraded target framework from .NET 9.0 to .NET 10.0
+ Updated plugin download URLs from neo-modules to neo-node repository
+ Set version to 1.7.0 in project file

### Fixed

+ Fixed scanHeight initial value display issue: when headerHeight is 0, scanHeight should also display as 0 instead of 1
+ Fixed unit test failures:
  - TrackDB_Test.AddTransfer_Test: Fixed invalid TxId format and missing contract entity
  - BlockInvoker_Test and WalletInvoker_Test: Added fallback NeoSystem for test scenarios
+ Fixed API compatibility issues with Neo 3.9.1:
  - Updated Blockchain.Import constructor usage
  - Updated UnclaimedGas and GetExecFeeFactor calls to use ApplicationEngine
  - Fixed Transaction.Witnesses required property initialization

##  [Previous]

Compatbile with Neo3-preview3: [Neo CLI v3.0.0-preview3](https://github.com/neo-project/neo-node/releases/tag/v3.0.0-preview3)

### Added

##### 1. Blockchain Data

+ asset list & related transactions
+ support converting Hex to Opcode for Witness & script in transaction

##### 2. Wallet

+ support multi-signature address 
+ allow multiple transfers
+ support batch transfer
+ display tx detail for pending transactions
+ change passoword

##### 3. Smart Contract support

+ add smart contract detail 
+ analyize and display all txs related to certain smart contract
+ support switching data types for params when invoking contract
+ support contract migration and destroy

##### 4. Advanced features

+ transaction signing & bradcast for multi-signature address

### Updated

+ Update backend node to be compatible with Neo3-Preview3

+ Optimized tx analysis to identify more contract related txs
+ Not allow claim gas again when it is clicked



##  [Neo3-GUI 1.0.0] for Neo3-preview2 

### Announcements
This is the initial release of Neo3-GUI with compatibility with neo3-preview2 testnet. Considering this is the first release of Neo3-GUI, it covers majority (but not all) of basic functions as a neo node with user interface. Users and developers are welcome to get your hands on it early, your feedbacks and suggestions are welcome.


> We will keep adding or updating features corresponding to neo3 development as well as improving the user experience for Neo3-GUI.

### Dependencies

Please install [.NET Core 3.1](https://dotnet.microsoft.com/download)  first.

### Features Added
#### 1. Blockchain Data
+ Check the list of blocks and detailed information for each block and its transactions.
+ Search block by block height.
+ Check the list of confirmed and pending transactions on blockchain.
+ Check the transaction details, includes basic info and application logs.
+ Get the list of issued assets and their related transactions.
#### 2. Wallet
+ Creating a new Neo3 wallet or open the exist one.
+ Import a exist wallet with private key or encrypted nep2 key.
+ Query the balance of all use's assets in a wallet.
+ Display the wallet details, includes public key, private key and WIF.
+ Transfer assets.
+ Query related transactions.
+ Adding or deleting account/address from the wallet.
#### 3. Smart Contract support
+ Search smart contract by its hash.
+ Display the basic info of a smart contract.
+ Deploy smart contract with nef & manifest files.
+ Invoke smart contract via manifest
#### 4. Advanced features
+ Vote for consensus node.
+ Register as a candidate for consensus node.
+ Data convertor between string and hexString, also wallet address and script hash.

#### 5. Setting
+ Multi-language: support Chinese and English.
+ Switch network between testnet & private chain.


| File Hash                                                    |
| ------------------------------------------------------------ |
| File Name: Neo-GUI.1.0.0.exe <br/> SHA1: 21e93d28ac4b2a30562e494a26732a71de6b8bf4 <br/> SHA256: e6def79b7df30a3c03fea90404946322822547371a68052685c2d574f7493e7d |
| File Name: Neo-GUI-1.0.0.dmg <br/> SHA1: 2d19884aaa76c88ac72c5185712d19b1ce509e76 <br/> SHA256: f471c57822036025a1f3b88a67e8b6a117580451edde92039bd3f697e079bd5a |