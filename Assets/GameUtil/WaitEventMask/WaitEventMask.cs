using System;

public static class WaitEventMask {
    public static void Wait(EventGroup groupId, short eventId, float waitTime = 5f, Action callbackSuccess = null, Action callbackTimeout = null) {
        EventCountDown ecd = new EventCountDown(groupId, eventId);
        ecd.Wait(
            () => {
                closeMask();
                callbackSuccess?.Invoke();
            },
            () => {
                closeMask();
                timeoutAction(callbackTimeout);
            },
            waitTime);
        // TODO£ºUI Mask
        // ui.UiManager.instance.OpenUi<ui.WaitForResponseMask>();
    }

}