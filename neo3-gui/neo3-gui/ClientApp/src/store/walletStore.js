
/* eslint-disable */
import { observable, action } from "mobx";

class WalletStore {
    @observable isOpen = false;
    @observable accountlist = [];

    @action logout() {
        this.isOpen = false;
        this.accountlist = [];
    }

    @action setWalletState(isopen) {
        console.log("set wallet:", isopen);
        this.isOpen = isopen;
    }

    @action setAccounts(data) {
        this.accountlist = data;
    }

    @action addAccount(account) {
        this.accountlist = this.accountlist.concat(account);
    }

}

export default WalletStore;