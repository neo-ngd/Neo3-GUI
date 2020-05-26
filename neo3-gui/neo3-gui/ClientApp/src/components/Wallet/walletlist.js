/* eslint-disable */
import React from 'react';
import { Link } from 'react-router-dom';
import { observer, inject } from "mobx-react";
import axios from 'axios';
import { Layout, message, Row, Col, List, Avatar, Button, Typography,PageHeader,Modal,Input } from 'antd';
import '../../static/css/wallet.css'
import Sync from '../sync';
import { withTranslation } from "react-i18next";

import {
  PlusCircleOutlined
} from '@ant-design/icons';

const { Content } = Layout;

@withTranslation()
@inject("walletStore")
@observer
class Walletlist extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      size: 'default',
      assetlist: [],
      iconLoading: false
    };
  }
  componentDidMount() {
    this.getAddress();
    this.getAllasset();
    this.getGas();
  }
  getAllasset = () => {
    var _this = this;
    axios.post('http://localhost:8081', {
      "id": "12",
      "method": "GetMyTotalBalance",
      "params": {}
    })
    .then(function (response) {
      var _data = response.data;
      if (_data.msgType === -1) {
        console.log("GetMyTotalBalance Error");
        console.log(_data);
        return;
      }
      _this.setState({
        assetlist: _data.result
      })
    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  getAddress = () => {
    var _this = this;
    axios.post('http://localhost:8081', {
      "id": "1234",
      "method": "ListAddress",
      "params": {
        "count": 10
      }
    })
    .then(function (response) {
      var _data = response.data;
      if (_data.msgType === -1) {
        console.log("ListAddress Error");
        console.log(_data);
        return;
      }
      _this.props.walletStore.setAccounts(_data.result.accounts);
    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  getGas = () => {
    var _this = this;
    axios.post('http://localhost:8081', {
      "id": 51,
      "method": "ShowGas"
    })
    .then(function (response) {
      var _data = response.data;
      if (_data.msgType == -1) {
        console.log("ShowGas Error");
        console.log(_data);
        return;
      }

      _this.props.walletStore.setUnclaimedGas(_data.result.unclaimedGas);

    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  claimGas = () => {
    const { t } = this.props;
    this.setState({iconLoading:true})
    setTimeout(this.setState({iconLoading:false}),10000);
    axios.post('http://localhost:8081', {
      "id": 51,
      "method": "ClaimGas"
    })
    .then(function (response) {
      var _data = response.data;
      if (_data.msgType === -1) {
        console.log("ClaimGas Error");
        console.log(_data);
        message.success(t("wallet.gas fail"), 3);
        return;
      } else if (_data.msgType = 3) {
        message.success(t("wallet.gas success"), 3);
      }
    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  addAddress = () => {
    const { t } = this.props;
    var _this = this;
    axios.post('http://localhost:8081', {
      "id": "1",
      "method": "CreateAddress"
    })
    .then(function (response) {
      var _data = response.data;
      if (_data.msgType === -1) {
        message.error(t('wallet.open wallet first'));
        console.log(_data)
        return;
      }
      message.success(t('wallet.add address success'));
      _this.props.walletStore.addAccount(_data.result);
    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  importPrivate = () => {
    const { t } = this.props;
    this.handleCancel();

    var pass = document.getElementById("privateKey").value;
    axios.post('http://localhost:8081', {
      "id": "20",
      "method": "ImportAccounts",
      "params": [pass]
    })
    .then(function (res) {
      let _data = res.data;
      if (_data.msgType === 3) {
        message.success(t('wallet.import private success'), 2);
      } else {
        message.info(t('wallet.private fail'), 2);
      }
    })
    .catch(function (error) {
      console.log(error);
      console.log("error");
    });
  }
  showModal = () => {
    const { t } = this.props;
    this.setState({
      visible: true,
      modalPanel:<Private func={this.importPrivate} t={t}/>,
      modalTitle:t("wallet.import private")
    });
  };
  handleOk = () => {
    this.setState({
      visible: false,
    });
  };
  handleCancel = () => {
    this.setState({
      visible: false,
    });
  };
  render() {
    const accounts = this.props.walletStore.accountlist;
    const unclaimedGas = this.props.walletStore.unclaimedGas;

    const { assetlist } = this.state;
    const { t } = this.props;
    return (
      <Layout className="gui-container">
        <Sync />
        <Content className="mt3">
          <Row className="mb2" gutter={[30, 0]} type="flex" style={{ 'minHeight': 'calc( 100vh - 120px )' }}>
            <Col span={13} className="bg-white pv4">
              <div className="in-title">
                <h2 className="mb0">
                  {t("wallet.accounts")}
                  <div className="wal-import float-r">
                      <PlusCircleOutlined className=""/>
                      <div className="wal-ul">
                        <ul>
                          <li><a onClick={this.addAddress}>{t('wallet.add address')}</a></li>
                          <li><a onClick={this.showModal}>{t('wallet.import private')}</a></li>
                        </ul>
                      </div>
                  </div>
                </h2>
              </div>
              <List
                itemLayout="horizontal"
                dataSource={accounts}
                renderItem={item => (
                  <List.Item>
                    <List.Item.Meta
                      title={<Link to={"/wallet/walletlist:" + item.address} title={t("wallet.show detail")}>{item.address}</Link>}
                      description={
                        <span className="f-s">
                          <span className="mr2">NEO <span className="wa-count">{item.neo}</span></span>
                          <span>GAS <span className="wa-count">{item.gas}</span></span>
                        </span>}
                    />
                  </List.Item>
                )}
              />
            </Col>
            <Col span={10} offset={1} className="bg-white pv4">
              <PageHeader title={t("wallet.assets")} ></PageHeader>
              <List
                className="asset-list"
                itemLayout="horizontal"
                style={{ 'minHeight': 'calc( 100% - 135px )' }}
                dataSource={assetlist}
                header={<div><span>{t("blockchain.asset info")} <small></small></span><span className="float-r wa-amount">{t("wallet.balance")}</span></div>}
                renderItem={item => (
                  <List.Item>
                    <List.Item.Meta
                      avatar={
                        <Avatar src={"https://neo.org/images/gui/"+item.asset+".png"}/>
                      }
                      title={<span className="upcase">{item.symbol}</span>}
                      description={<span className="f-xs">{item.asset}</span>}
                    />
                    <Typography>{item.balance}</Typography>
                  </List.Item>
                )}
              />
              <div className="w200 mt4">
                <Button className="w200" onClick={this.claimGas} loading={this.state.iconLoading}>{t("button.claim")} {unclaimedGas} GAS</Button>
              </div>
            </Col>
          </Row>
          <Modal
            width={400}
            centered
            title={this.state.modalTitle}
            visible={this.state.visible}
            onCancel={this.handleCancel}
            footer={null}
          >
          {this.state.modalPanel}
          </Modal>
        </Content>
      </Layout>
    );
  }
}

export default Walletlist;


const Private = ({func,t}) => {
  return (
    <div>
      <Input type="text" id="privateKey" placeholder={t("please input Hex/WIF private key")} />
      <p className="text-c mb0">
        <Button onClick={func} type="primary" className="mt3">{t("wallet.import private")}</Button>
      </p>
    </div>
  )
};