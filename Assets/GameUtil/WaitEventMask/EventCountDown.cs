using System;
using UnityEngine;

public class EventCountDown {
    private readonly EventGroup groupId;
    private readonly short eventId;

    public EventCountDown(EventGroup gId, short eId) {
        groupId = gId;
        eventId = eId;
    }



    public void Wait(Action callbackSuccess, Action callbackTimeout, float waitTime = 5f) {
        void eventSuccess() {
            stopTimeoutTimer();
            evt.EventManager.instance.Unregister(groupId, eventId, eventSuccess);
            callbackSuccess?.Invoke();
        }

        evt.EventManager.instance.Register(groupId, eventId, eventSuccess);
        startTimeoutTimer(callbackTimeout, waitTime, () => evt.EventManager.instance.Unregister(groupId, eventId, eventSuccess));
    }

    private void stopTimeoutTimer() {
        TimeManager.instance.RemoveTimer(getTimerName());
    }

    private string getTimerName() {
        return $"EventCountDown_{groupId}_{eventId}";
    }
}