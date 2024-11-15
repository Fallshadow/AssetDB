using System.Collections.Generic;
using System;
using UnityEngine;

public partial class TimeMgr : MgrBase , IUpdateListener {
    public static ulong ClientLocalTimeStamp { get { return Convert.ToUInt64(DateTimeOffset.Now.ToLocalTime().ToUnixTimeSeconds()); } }

    public void OnUpdate() {
        tempCurrentNode = timerLink.First;

        while (tempCurrentNode != null) {
            LinkedListNode<Timer> nextNode = tempCurrentNode.Next;
            if (tempCurrentNode.Value.IsUnscaled) {
                tempCurrentNode.Value.Process(Time.unscaledDeltaTime);
            }
            else {
                tempCurrentNode.Value.Process(Time.deltaTime);
            }
            tempCurrentNode = nextNode;
        }
    }

    public void Reset(bool bForce = false) {
        tempCurrentNode = timerLink.First;
        while (tempCurrentNode != null) {
            LinkedListNode<Timer> nextNode = tempCurrentNode.Next;

            if (tempCurrentNode.Value.isGlobal == false && (bForce == true || tempCurrentNode.Value.canReset)) {
                tempCurrentNode.Value.Reset();
                timerLink.Remove(tempCurrentNode);
            }
            tempCurrentNode = nextNode;
        }
    }
}