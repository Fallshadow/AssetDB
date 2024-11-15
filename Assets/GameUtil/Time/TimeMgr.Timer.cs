using System.Collections.Generic;
using System;
using UnityEngine;

public partial class TimeMgr {
    private LinkedList<Timer> timerLink = new LinkedList<Timer>();
    private LinkedListNode<Timer> tempCurrentNode = null;

    public Timer AddUpdateTimer(string name, bool isUnique, bool isUnscaled, Action<float> updateCallback, bool autoStart = true) {
        if (isUnique) {
            Timer existTimer = GetTimer(name);
            if (existTimer != null)
                return existTimer;
        }

        UpdateTimer updateTimer = new UpdateTimer(name, isUnique, isUnscaled, updateCallback);
        if (!autoStart) {
            updateTimer.Pause();
        }

        return addTimer(updateTimer);
    }

    public Timer AddCountDownUpdateTimer(string name, bool isUnique, bool isUnscaled, float countDownTime, Action<float> updateCallBack, Action callback, bool autoStart = true, bool autoRemove = true) {
        if (isUnique) {
            Timer existTimer = GetTimer(name);
            if (existTimer != null)
                return existTimer;
        }

        CountDownUpdateTimer countDownUpdateTimer = new CountDownUpdateTimer(name, isUnique, isUnscaled, countDownTime, autoRemove, updateCallBack, callback);
        if (!autoStart) {
            countDownUpdateTimer.Pause();
        }

        return addTimer(countDownUpdateTimer);
    }

    public Timer AddCountDownTimer(string name, bool isUnique, bool isUnscaled,  float countDownTime, Action callback, Action cbReset, bool autoStart = true, bool autoRemove = true) {
        if (isUnique) {
            Timer existTimer = GetTimer(name);
            if (existTimer != null) {
                return existTimer;
            }
        }

        CountDownTimer countDownTimer = new CountDownTimer(name, isUnique, isUnscaled, countDownTime, callback, cbReset, autoRemove);
        if (!autoStart) {
            countDownTimer.Pause();
        }

        return addTimer(countDownTimer);
    }

    public CycleTimer AddCycleTimer(string name, bool isUnique, bool isUnscaled, float cycle, Action callback, Action cbReset, bool autoStart = true) {
        if (isUnique) {
            Timer existTimer = GetTimer(name);
            if (existTimer != null)
                return (CycleTimer)existTimer;
        }

        CycleTimer cycleTimer = new CycleTimer(name, isUnique, isUnscaled, cycle, callback, cbReset);
        if (!autoStart) {
            cycleTimer.Pause();
        }

        return (CycleTimer)addTimer(cycleTimer);
    }

    public Timer GetTimer(string name) {
        tempCurrentNode = timerLink.First;
        while (tempCurrentNode != null) {
            if (tempCurrentNode.Value.Name != null && tempCurrentNode.Value.Name.Equals(name, StringComparison.Ordinal))
                return tempCurrentNode.Value;

            tempCurrentNode = tempCurrentNode.Next;
        }

        return null;
    }

    public void RemoveTimer(Timer timer) {
        if (timer != null) {
            timerLink.Remove(timer);
        }
    }

    // All same name will remove!
    public void RemoveTimer(string name) {
        tempCurrentNode = timerLink.First;
        while (tempCurrentNode != null) {
            LinkedListNode<Timer> nextNode = tempCurrentNode.Next;

            if (tempCurrentNode.Value.Name == name) {
                timerLink.Remove(tempCurrentNode);
            }

            tempCurrentNode = nextNode;
        }
    }

    private Timer addTimer(Timer data) {
        if (!timerLink.Contains(data)) {
            timerLink.AddLast(data);
            return data;
        }

        Debug.Log("[TimeMgr] Arealy have same timer!  =>" + data.Name);
        return null;
    }
}