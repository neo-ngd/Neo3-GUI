/* eslint-disable */
//just test replace wallet//
import React from 'react';
import { Layout, Modal, Radio } from 'antd';
import { observer, inject } from "mobx-react";
import { withRouter } from "react-router-dom";
import '../../static/css/wallet.css';
import { withTranslation } from 'react-i18next';
import Config from "../../config";
import { shell, remote } from "electron";
import neonode from "../../neonode";



@withTranslation()
@inject("walletStore")
@inject("blockSyncStore")
@observer
@withRouter
class Setting extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      size: 'default',
      address: "",
      addresslist: [],
      iconLoading: false,
      gas: 0,
    };
  }
  switchLang = (lng) => {
    const { t, i18n } = this.props;
    if (Config.Language === lng) {
      return;
    }
    i18n.changeLanguage(lng);
    Config.changeLang(lng);
  }
  switchNetwork = (network) => {
    // console.log("changing to network:" + network);
    // console.log(Config.Network);
    this.props.switchNetwork(network);
  }
  openUrl(url) {
    return () => {
      shell.openExternal(url);
    }
  }
  render = () => {
    const { t, i18n } = this.props;
    const { Network } = Config;
    return (
      <div>
        <h4>{t("settings.network")}</h4>
        
        <Radio.Group name="radiogroup" defaultValue={Network} onChange={(e) => this.switchNetwork(e.target.value)}>
          <Radio value="mainnet" disabled>{t("settings.mainnet")}</Radio>
          <Radio value="testnet">{t("settings.testnet")}</Radio>
          <Radio value="private">{t("settings.privatenet")}</Radio>
        </Radio.Group>
        <p className="small mt5">{t("settings.network info")}</p>

        <h4 className="mt3">{t("settings.language")}</h4>
        <Radio.Group className="setting-ul" defaultValue={i18n.language} onChange={(e) => this.switchLang(e.target.value)}>
          <Radio value="zh">中文</Radio>
          <Radio value="en">English</Radio>
        </Radio.Group>

        <h4 className="mt3">{t("settings.about")}</h4>
        <p className="font-s">{t("settings.version")} {remote.app.getVersion()}</p>

        <div className="mt1 mb3 text-c small">
          <p className="mb5 t-light">NeoGUI @ 2020 Neo-Project {t("settings.copyright")}</p>
          <p>
            <a className="mr3 t-green" onClick={this.openUrl("https://github.com/neo-ngd/Neo3-GUI/issues")}>{t("settings.report issues")}</a>
            <a className="t-green" onClick={this.openUrl("https://neo.org/")}>Neo{t("settings.website")}</a>
          </p>
        </div>
      </div>
    );
  }
}

export default Setting;