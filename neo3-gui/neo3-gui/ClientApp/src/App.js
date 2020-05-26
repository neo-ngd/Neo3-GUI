import React from "react";
import Router from './router/router';
import { ConfigProvider } from "antd";
import { Provider } from "mobx-react";
import stores from "./store/stores";
import Config from "./config";
import neoNode from "./neonode";


class App extends React.Component {
  constructor(props) {
    super(props);

    console.log(window.location.href);
    if (process.env.NODE_ENV !== "development") {
      neoNode.switchNode();
    }
    this.initWebSocket();
  }


  initWebSocket = () => {
    console.log("connecting");
    this.ws = new WebSocket('ws://127.0.0.1:8081');

    this.ws.onopen = () => {
      console.log('opened');
    };

    this.ws.onclose = (e) => {
      console.log("closed:", e);
      this.reconnectWebSocket();
    }

    this.ws.onerror = (e) => {
      console.log("error:", e);
    }

    this.ws.onmessage = this.processMessage;
  };


  reconnectWebSocket = () => {
    let self = this;
    if (self.lock) {
      return;
    }
    self.lock = true;
    setTimeout(() => {
      self.initWebSocket();
      self.lock = false;
    }, 3000);
  }


  processMessage = (message) => {
    var msg = JSON.parse(message.data);
    switch (msg.method) {
      case "getSyncHeight":
        stores.blockSyncStore.setHeight(msg.result);
        break;
      case "getWalletBalance":
        stores.walletStore.setAccounts(msg.result);
        break;
      default:
        break;
    }
  }

  render() {
    return (
      <Provider {...stores}>
        <ConfigProvider>
          <Router></Router>
        </ConfigProvider>
      </Provider>
    );
  }
}


export default App;
