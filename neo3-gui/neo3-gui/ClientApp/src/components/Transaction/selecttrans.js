/* eslint-disable */
import React from 'react';
import { observer, inject } from "mobx-react";
import 'antd/dist/antd.css';
import {
  Alert,
  Tabs,
  Row,
  Col
} from 'antd';
import { Layout } from 'antd';
import '../../static/css/wallet.css';
import Multitomulti from './multitomulti';
import Onetomulti from './onetomulti';
import Sync from '../sync';
import { post } from "../../core/request";

import { withTranslation } from "react-i18next";

const { Content } = Layout;
const { TabPane  } = Tabs;

@withTranslation()
@inject("walletStore")
@observer
class Transfer extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      addresslist: []
    };
  }
  componentDidMount() {
    var _this = this;
    const { t } = this.props;
    post("GetMyBalances",{}).then(res =>{
      var _data = res.data;
      if (_data.msgType === -1) {
        message.error(t("wallet.open wallet first"));
        return false;
      }
      _this.setState({
        addresslist: _data.result
      })
    })
  }
  render() {
    const { t } = this.props;
    const { addresslist } = this.state;
    return (
      <Layout className="gui-container">
        <Sync />
        <Content className="mt3">
          <Row gutter={[30, 0]} className="mb1">
            <Col span={24} className="bg-white pv4">
              <Tabs className="tran-title" defaultActiveKey="1">
                <TabPane tab={t("转账")} key="1">
                  <Multitomulti account={addresslist}  />
                </TabPane>
                <TabPane tab={t("批量转账")} key="2">
                  <Onetomulti account={addresslist}  />
                </TabPane>
              </Tabs>
              <Alert
                className="mt2 mb4"
                showIcon
                type="info"
                message={t("wallet.transfer warning")}
              />
            </Col>
          </Row>
        </Content>
      </Layout>
    );
  }
}

export default Transfer;