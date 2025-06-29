﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.Common.Scanners;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.SmartContract.Native;

namespace Neo.Models.Jobs
{
    public class SyncHeightJob : Job
    {
        public SyncHeightJob(TimeSpan timeSpan)
        {
            IntervalTime = timeSpan;
        }
        public override async Task<WsMessage> Invoke()
        {
            uint scanHeight = ExecuteResultScanner.ScanHeight;
            uint height = this.GetCurrentHeight();
            uint headerHeight = this.GetCurrentHeaderHeight();
            return new WsMessage()
            {
                MsgType = WsMessageType.Push,
                Method = "getSyncHeight",
                Result = new HeightStateModel
                {
                    ScanHeight = scanHeight,
                    SyncHeight = height,
                    HeaderHeight = headerHeight,
                    ConnectedCount = this.GetDefaultLocalNode().ConnectedCount
                }
            };
        }
    }
}
