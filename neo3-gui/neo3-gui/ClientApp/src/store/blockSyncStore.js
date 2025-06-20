/* eslint-disable */
import { observable, action, makeObservable } from "mobx";

class BlockSyncStore {
  constructor() {
    makeObservable(this);
  }

  @observable scanHeight = -1;
  @observable syncHeight = -1;
  @observable headerHeight = -1;

  @action setHeight(data) {
    this.scanHeight = data.scanHeight
    this.syncHeight = data.syncHeight;
    this.headerHeight = data.headerHeight;
  }
}

export default BlockSyncStore;
