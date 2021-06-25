import React from 'react';
import {BrowserRouter, Route , Switch,Redirect} from 'react-router-dom';
import Home from '../pages/home'
import Sync from '../components/sync';

import Chain from '../components/Chain/chain';
import Chainlayout from '../components/Chain/chainlayout';
import Blockdetail from '../components/Chain/blockdetail';
import Blockhashdetail from '../components/Chain/hashdetail';
import Chaintrans from '../components/Chain/trans';
import Chainasset from '../components/Chain/asset';
import Assetdetail from '../components/Chain/assetdetail';

import Walletlayout from '../components/Wallet/walletlayout';
import Walletlist from '../components/Wallet/walletlist';
import Walletdetail from '../components/Wallet/walletdetail';
import Wallettrans from '../components/Wallet/trans';

import Selecttrans from '../components/Transaction/selecttrans';
import Transdetail from '../components/Transaction/transdetail';
import Untransdetail from '../components/Transaction/untransdetail';


import Contract from '../components/Contract/contract';
import Contractlayout from '../components/Contract/contractlayout';
import Contractdeploy from '../components/Contract/deploy';
import ContractUpgrade from '../components/Contract/upgrade';
import Contractinvoke from '../components/Contract/invoke';
import Contractdetail from '../components/Contract/contractdetail';

import Advanced from '../components/Advanced/advanced';
import Advancedlayout from '../components/Advanced/advancedlayout';
import Advancedvote from '../components/Advanced/vote';
import Advancedcandidate from '../components/Advanced/candidate';
import Advancedsignature from '../components/Advanced/signature';
import Advancedcommittee from '../components/Advanced/committee';
import Advanceddesignrole from '../components/Advanced/designrole';
import Advancednoderole from '../components/Advanced/noderole';

import { Authenticated } from '../core/authentication';

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
                    <Route exact path="/chain/untransaction:hash" component={Untransdetail} />
                    <Route exact path="/chain/asset" component={Chainasset} />
                    <Route exact path="/chain/asset:hash" component={Assetdetail} />
                </Layout>
            </Route>
            <Route path="/wallet">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route  component={Walletlayout} />
                    <Route exact path="/wallet/walletlist" component={Authenticated(Walletlist)} />
                    <Route exact path="/wallet/walletlist:address" component={Authenticated(Walletdetail)} />
                    <Route exact path="/wallet/address:address" component={Authenticated(Walletdetail)} />
                    <Route exact path="/wallet/transaction" component={Authenticated(Wallettrans)} />
                    <Route exact path="/wallet/transaction:hash" component={Authenticated(Transdetail)} />
                    <Route exact path="/wallet/untransaction:hash" component={Authenticated(Untransdetail)} />
                    <Route exact path="/wallet/transfer" component={Authenticated(Selecttrans)} />
                </Layout>
            </Route>
            <Route path="/contract">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Contractlayout} />
                    <Route exact path="/contract" component={Contract} />
                    <Route exact path="/contract/detail:hash" component={Contractdetail} />
                    <Route exact path="/contract/deploy" component={Authenticated(Contractdeploy)} />
                    <Route exact path="/contract/upgrade" component={Authenticated(ContractUpgrade)} />
                    <Route exact path="/contract/invoke" component={Authenticated(Contractinvoke)} />
                </Layout>
            </Route>
            <Route path="/advanced">
                <Layout style={{ height: 'calc( 100vh )'}}>
                    <Route component={Advancedlayout} />
                    <Route exact path="/advanced" component={Advanced} />
                    <Route exact path="/advanced/vote" component={Authenticated(Advancedvote)} />
                    <Route exact path="/advanced/candidate" component={Authenticated(Advancedcandidate)} />
                    <Route exact path="/advanced/signature" component={Authenticated(Advancedsignature)} />
                    <Route exact path="/advanced/committee" component={Authenticated(Advancedcommittee)} />
                    <Route exact path="/advanced/designrole" component={Advanceddesignrole} />
                    <Route exact path="/advanced/getnoderole" component={Advancednoderole} />
                </Layout>
            </Route>
            <Redirect from="*" to="/" />
        </Switch>
    </BrowserRouter>
);

export default BasicRoute;