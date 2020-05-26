/* eslint-disable */
import React from 'react';
import { observer, inject } from "mobx-react";
import {
  Alert, Input,
  Form,
  InputNumber,
  Modal,
  Select,
  Row,
  Col,
  message,
  Button,
  AutoComplete,
} from 'antd';
import { Layout } from 'antd';
import '../../static/css/wallet.css'
import { withTranslation } from "react-i18next";
import { post } from "../../core/request";

const { Option } = Select;
const { Content } = Layout;

@withTranslation()
class Onetomulti extends React.Component{
    constructor(props) {
      super(props);
      this.state = {
        iconLoading: false,
        selectadd:[]
      };
    }
    setAddress = target => {
      console.log(this.refs.formRef)

      target = target ? target : 0;
      let _detail = this.props.account[target].balances;
      this.setState({
        selectadd: _detail
      })
    }
    transfer = values =>{
        var _this = this;
        const {t}=this.props;
        if(values.receiver.trim() === ""){
            message.error("收款地址为空");
            return false;
        }

        var receivers = [];
        var addlist = values.receiver.trim().split(/\n+/);
        addlist.map(item=>{
            let _item = item.trim().split(/\s+/);
            _item.filter(cur => {return cur !== null && cur !== undefined;})
            if(_item.length !== 2){message.error("输入错误请重新输入");return false;}
            
            let receiver = {};
            receiver.address = _item[0];
            receiver.amount = _item[1];
            receivers.push(receiver);
        })

        var params = {
            "sender":this.props.account[values.sender].address,
            "receivers":receivers,
            "asset":values.asset
        };

        post("SendToMultiAddress",params).then(res =>{
            var _data = res.data;
            var result = res.data.result;
            if(_data.msgType === -1){
                let res = _data.error;
                Modal.error({
                title: t('wallet.transfer send error'),
                width: 400,
                content: (
                    <div className="show-pri">
                        <p>{t("error code")}: {res.code}</p>
                        <p>{t("error msg")}: {res.message}</p>
                    </div>
                ),
                okText:"确认"
                });
                return;
            }else{
                Modal.success({
                title: t('wallet.transfer send success'),
                content: (
                    <div className="show-pri">
                    <p>{t("blockchain.transaction hash")}：{result.txId}</p>
                    </div>
                ),
                okText:"确认"
                });
                _this.refs.formRef.resetFields()
                _this.setState({
                    selectadd:[]
                })
            }
        })
    }
    render = () =>{
        const {account} = this.props;
        const {selectadd} = this.state;
        const { t } = this.props;
        return (
        <Form ref="formRef" className="trans-form" onFinish={this.transfer}>
            <Row gutter={[30, 0]} className="bg-white pv4">
                <Col span={24}>
                <div className="w500 mt3" style={{ 'minHeight': 'calc( 100vh - 350px )' }}>
                    <Row>
                      <Col span={15}>
                      <Form.Item
                        name="sender"
                        label={t("付款账户")}
                        rules={[
                            {
                            required: true,
                            message: t("wallet.please select a account"),
                            },
                        ]}
                        >
                        <Select
                            placeholder={t("select account")}
                            style={{ width: '100%' }}
                            onChange={this.setAddress}>
                            {account.map((item, index) => {
                            return (
                                <Option key={index}>{item.address}</Option>
                            )
                            })}
                        </Select>
                      </Form.Item>
                      </Col>
                      <Col span={9}>
                          <Form.Item
                          name="asset"
                          label={t("wallet.asset")}
                          rules={[
                              {
                              required: true,
                              message: t("wallet.required"),
                              },
                          ]}>
                          <Select
                              placeholder={t("wallet.select")}
                              style={{ width: '100%' }}
                          >
                              {selectadd.map((item, index) => {
                              return (
                                  <Option key={index} value={item.asset} title={item.symbol}><span className="trans-symbol">{item.symbol} </span><small>{item.balance}</small></Option>
                              )
                              })}
                          </Select>
                          </Form.Item>
                      </Col>
                    </Row>
                    
                    <Form.Item
                    name="receiver"
                    label={t("wallet.to")}
                    rules={[
                        {
                        required: true,
                        message: t("wallet.please input a account"),
                        },
                    ]}
                    >
                    <Input.TextArea placeholder={t("输入地址和金额，用空格分隔 例如：AKMqDy51FuMnc4poiGBczQvPh6819hQuLH 10 AKMqDy51FuMnc4poiGBczQvPh6819hQuLH 10")} />
                    </Form.Item>
                    <div className="text-c lighter">
                        <small>{t("wallet.estimated time")}：12s </small>
                    </div>
                    <Form.Item>
                    <Button type="primary" htmlType="submit" loading={this.state.iconLoading}>
                        {t("button.send")}
                    </Button>
                    </Form.Item>
                </div>
                </Col>
            </Row>
        </Form>
        );
    }
} 

export default Onetomulti;