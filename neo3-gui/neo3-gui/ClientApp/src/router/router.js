import React from 'react';
import {BrowserRouter, Route, Switch,Redirect} from 'react-router-dom';
import Home from '../pages/home'
import Sync from '../components/sync';
import Addressdetail from '../components/Common/addressdetail';

import Chain from '../components/Chain/chain';
import Chainlayout from '../components/Chain/chainlayout';
import Blockdetail from '../components/Chain/blockdetail';
import Blockhashdetail from '../components/Chain/hashdetail';
import Chaintrans from '../components/Chain/trans';
import Chainasset from '../components/Chain/asset';
import Assetdetail from '../components/Chain/assetdetail';

import Wallet from '../components/Wallet/wallet';
import Walletlayout from '../components/Wallet/walletlayout';
import Walletlist from '../components/Wallet/walletlist';
import Walletdetail from '../components/Wallet/walletdetail';
import Wallettrans from '../components/Wallet/trans';

import Transfer from '../components/Transaction/transfer';
import Selecttrans from '../components/Transaction/selecttrans';
import Transdetail from '../components/Transaction/transdetail';

import Contract from '../components/Contract/contract';
import Contractlayout from '../components/Contract/contractlayout';
import Contractdeploy from '../components/Contract/deploy';
import Contractinvoke from '../components/Contract/invoke';
import Contractdetail from '../components/Contract/contractdetail';

import Advanced from '../components/Advanced/advanced';
import Advancedlayout from '../components/Advanced/advancedlayout';
import Advancedvote from '../components/Advanced/vote';
import Advancedcandidate from '../components/Advanced/candidate';

import { Layout } from 'antd';

const BasicRoute = () => (
    <BrowserRouter>
        <Switch>
            <Route exact path="/" component={Home}/>
            <Route exact path="/sync" component={Sync}/>
            <Route path="/chain">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Chainlayout} />
                    <Route exact path="/chain" component={Chain} />
                    <Route exact path="/chain/detail:height" component={Blockdetail} />
                    <Route exact path="/chain/hashdetail:height" component={Blockhashdetail} />
                    <Route exact path="/chain/transaction" component={Chaintrans} />
                    <Route exact path="/chain/transaction:hash" component={Transdetail} />
                    <Route exact path="/chain/asset" component={Chainasset} />
                    <Route exact path="/chain/asset:hash" component={Assetdetail} />
                    <Route exact path="/chain/address:address" component={Addressdetail} />
                </Layout>
            </Route>
            <Route path="/wallet">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Walletlayout} />
                    <Route exact path="/wallet" component={Wallet} />
                    <Route exact path="/wallet/walletlist" component={Walletlist} />
                    <Route exact path="/wallet/walletlist:address" component={Walletdetail} />
                    <Route exact path="/wallet/address:address" component={Walletdetail} />
                    <Route exact path="/wallet/transaction" component={Wallettrans} />
                    <Route exact path="/wallet/transaction:hash" component={Transdetail} />
                    {/* <Route exact path="/wallet/transfer" component={Transfer} /> */}
                    <Route exact path="/wallet/transfer" component={Selecttrans} />
                </Layout>
            </Route>
            <Route path="/contract">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Contractlayout} />
                    <Route exact path="/contract" component={Contract} />
                    <Route exact path="/contract/detail:hash" component={Contractdetail} />
                    <Route exact path="/contract/deploy" component={Contractdeploy} />
                    <Route exact path="/contract/invoke" component={Contractinvoke} />
                    <Route exact path="/contract/wallet" component={Wallet} />
                </Layout>
            </Route>
            <Route path="/advanced">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Advancedlayout} />
                    <Route exact path="/advanced" component={Advanced} />
                    <Route exact path="/advanced/vote" component={Advancedvote} />
                    <Route exact path="/advanced/candidate" component={Advancedcandidate} />
                    <Route exact path="/advanced/wallet" component={Wallet} />
                </Layout>
            </Route>
            <Redirect from="*" to="/" />
        </Switch>
    </BrowserRouter>
);

export default BasicRoute;