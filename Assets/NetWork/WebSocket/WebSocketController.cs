using Google.Protobuf;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityWebSocket;

public class WebSocketController {

    WebSocket mainWS;

    Action reconnectCallBack = null;
    bool isReConnected;

    private readonly Dictionary<int, List<Action<object>>> events = new Dictionary<int, List<Action<object>>>();

    public void InitConnect(string mainAddress) {
        mainWS = new WebSocket(mainAddress);
        mainWS.OnOpen += MainSocket_OnOpen;
        mainWS.OnClose += MainSocket_OnClose;
        mainWS.OnMessage += MainSocket_OnMessage;
        mainWS.OnError += MainSocket_OnError;
        mainWS.ConnectAsync();
    }

    public void ReConnect(Action callback) {
        Debug.Log($"[WebSocket][ReConnect] {mainWS.ReadyState}");
        if (mainWS.ReadyState != WebSocketState.Open) {
            mainWS.ConnectAsync();
            isReConnected = true;
            reconnectCallBack = callback;
        }
    }

    public void CloseConnect() => mainWS.CloseAsync();

    public void Send(NetEvent netEvent, IMessage msg = null) {
        // mainWS.SendAsync(msg);
    }

    public void Listen(NetEvent netEvent, Action<object> action) {
        int eventID = (int)netEvent;
        if (events.TryGetValue(eventID, out List<Action<object>> listAction) == false) {
            events[eventID] = new List<Action<object>> { action };
            return;
        }
        listAction.Add(action);
    }

    public void RemoveListen(NetEvent netEvent, Action<object> action) {
        int eventID = (int)netEvent;
        if (events.TryGetValue(eventID, out List<Action<object>> listAction) == false) {
            return;
        }
        listAction.Remove(action);
    }

    private void MainSocket_OnOpen(object sender, OpenEventArgs e) {
        Debug.Log($"[WebSocket][Socket_OnOpen]");
        if (isReConnected) {
            isReConnected = false;
            reconnectCallBack?.Invoke();
        }
    }

    private void MainSocket_OnMessage(object sender, MessageEventArgs e) {
        // TODO : 从消息中获取 eventID pbobject
        //if (events.TryGetValue(eventID, out List<Action<object>> listAction) == false) {
        //    Debug.Log($"[WebSocket][OnMessage] 此 {eventID} 无人注册 不执行回调");
        //    return;
        //}
        //foreach (var action in listAction) {
        //    action?.Invoke(pbobject);
        //}
    }

    private void MainSocket_OnClose(object sender, CloseEventArgs e) {
        Debug.Log($"[WebSocket][OnClose] {e.StatusCode} {e.Reason}");
    }

    private void MainSocket_OnError(object sender, ErrorEventArgs e) {
        Debug.Log($"[WebSocket][OnError] {e.Message}");
    }
}