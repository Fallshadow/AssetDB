using UnityEngine;
using UnityEngine.UI;
using UnityWebSocket;
using System.Collections;
using UnityEngine.Networking;
using System;
using Google.Protobuf;

public class NetManager : MonoBehaviour {
    public static NetManager instance { get; private set; }
    WebSocketController wsController = new();

    const string wxLoginAddress = "https://zq.xmfunny.com/zqLogin";
    const string mainAddress = "wss://zq.xmfunny.com/zqGame";

    private float heartbeatInterval = 10f; // 心跳间隔时间（秒）
    private float timer = 0f; // 计时器

    public void Awake() {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        WebSocketConnect();
    }

    private void Update() {
        timer += Time.deltaTime;

        if (timer > heartbeatInterval) {
            // SendHeartRequest();
            timer = 0f;
        }
    }

    private void OnDestroy() {
        WebSocketClose();
    }

    public void WebSocketConnect() => wsController.InitConnect(mainAddress);

    public void WebSocketReConnect(Action callback) => wsController.ReConnect(callback);

    public void WebSocketClose() => wsController.CloseConnect();

    public void WsSend(NetEvent netEvent, IMessage msg = null) => wsController.Send(netEvent, msg);

    public void WsListen(NetEvent netEvent, Action<object> pb) => wsController.Listen(netEvent, pb);

    public void WsRemoveListen(NetEvent netEvent, Action<object> pb) => wsController.RemoveListen(netEvent, pb);

    public void DownloadImage(string imageUrl, RawImage showImage) {
        StartCoroutine(DownloadImageWeb(imageUrl, showImage));
    }

    IEnumerator DownloadImageWeb(string url, RawImage showImage) {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url)) {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success) {
                Debug.LogError("[NetManager][ImageWeb] Error downloading image: " + uwr.error);
            }
            else {
                showImage.texture = DownloadHandlerTexture.GetContent(uwr);
            }
        }
    }
}