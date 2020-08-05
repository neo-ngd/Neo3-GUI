
/* eslint-disable */
import React from 'react';
import { withRouter } from 'react-router-dom';
import { Typography, Button } from 'antd';
import 'antd/dist/antd.css';

import {
  SyncOutlined, 
  ArrowLeftOutlined
} from '@ant-design/icons';
import { observer, inject } from "mobx-react";
import { withTranslation } from "react-i18next"

const { Text } = Typography;

@withTranslation()
@inject("blockSyncStore")
@observer
class Sync extends React.Component {

  render() {
    const { t } = this.props;
    const { syncHeight, headerHeight } = this.props.blockSyncStore;
    const location = this.props.location.pathname;

    var topBarStyle = {
      display: 'flex',
      flexDirection: 'row-reverse',
      justifyContent: 'space-between',
      alignItems: 'center'
    };

    console.log(location);
    return (
      <div className="topBar" style={topBarStyle}>
        <div className="mr3 mb0">
          {
            headerHeight < 0
              ? <Text className="t-normal bold"> - / - {t("common.connecting")}</Text>
              : <Text className="t-normal bold"> {syncHeight} / {headerHeight} {t("common.syncing")}</Text>
          }
          <SyncOutlined className="ml3" type="sync" spin />
        </div>
        <div className="ml3" style={{ display: (location === '/' || location.indexOf(':') < 0 ) ? 'none' : 'default' }}>
          <Button
            type="link"
            style={{ color: '#00AF92', textAlign: 'left' }}
            icon={<ArrowLeftOutlined />}
            onClick={this.props.history.goBack}
          >
            Back
          </Button>
        </div>
      </div>
    );
  }
}

export default withRouter(Sync);