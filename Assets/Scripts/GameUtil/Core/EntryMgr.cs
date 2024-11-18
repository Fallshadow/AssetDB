using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// ������Ϸǰ��һЩ����
// ������ɺ������Ϸ
public class EntryMgr : MonoBehaviour
{
    public static EntryMgr instance { get; private set; }

    public GameSetting gameSetting { get; private set; }
    private UnityWebRequest gameSettingWebRequest;
    private bool isGameSettingLoadCompleted;
    private bool isGameSettingInitCompleted;

    public bool isDebugHttp = false;
    private const string tfHostSuffix = "";
    private const string devHostSuffix = "";
    private const string wxLoginServerHostDev = "" + devHostSuffix;
    private const string wxLoginServeHostDebug = "";

    private const string getIPAddressServe = "";
    private UnityWebRequest ipAddressWebRequest;
    private string ipAddress;

    public ResHelper resHelper;

    public static WxSdkMgr wxSdk;
    private bool isWxSdkLoginInitCompleted;

#if UNITY_EDITOR
    public string fakeCode = "";
#endif

    private void Awake() {
        instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        wxSdk = new WxSdkMgr();
        // ��ȡ���� IP ����У�������
        if (string.IsNullOrEmpty(getIPAddressServe)) {
            GameSettingInit(false);
        }
        else {
            GetIPAddress();
        }

#if WECHAT && !UNITY_EDITOR
        wxSdk.InitSDK(() => {
            wxSdk.Login(() => {
                isWxSdkLoginInitCompleted = true;
                OnApplicationBasicInitComplete();
            });
        });
#endif

#if UNITY_EDITOR
        isWxSdkLoginInitCompleted = true;
        if (String.IsNullOrEmpty(fakeCode)) {
            string deviceID = UnityEngine.SystemInfo.deviceUniqueIdentifier;
            Debug.Log("Device Unique Identifier: " + deviceID);
            wxSdk.SetFakeWxCode(deviceID);
        }
        else {
            wxSdk.SetFakeWxCode(fakeCode);
        }

        OnApplicationBasicInitComplete();
#endif
    }

    private void Update() {
        IPAddresTick();
        GameSettingTick();
    }

    #region GameSetting

    void GameSettingInit(bool isWhiteList) {
        string gameSettingUrl = string.Empty;

#if UNITY_EDITOR
        if (isWhiteList) {
            gameSettingUrl = $"file://{Application.dataPath}/Settings/game_setting_whitelist.json";
        }
        else {
            gameSettingUrl = $"file://{Application.dataPath}/Settings/game_setting.json";
        }
#else
        // ����Զ����Դ·��
        if (isWhiteList) {
            gameSettingUrl = $"{resHelper.remoteConfig.settingUrl}/game_setting_whitelist.json";
        } else {
            gameSettingUrl = $"{resHelper.remoteConfig.settingUrl}/game_setting.json";
        }
#endif
        Debug.Log($"[GameSetting][Init] {gameSettingUrl}");
        gameSettingWebRequest = UnityWebRequest.Get(gameSettingUrl);
        gameSettingWebRequest.SendWebRequest();
    }

    void GameSettingTick() {
        if (!isGameSettingLoadCompleted && gameSettingWebRequest != null) {
            if (gameSettingWebRequest.isDone) {
                isGameSettingLoadCompleted = true;

                if (gameSettingWebRequest.result != UnityWebRequest.Result.Success) {
                    Debug.LogError($"[GameSetting][Response] ��ȡ GameSetting ʧ�� {gameSettingWebRequest.error}");

                    gameSetting = new GameSetting() {
                        serverHost = wxLoginServerHostDev
                    };
                }
                else {
                    string responseText = gameSettingWebRequest.downloadHandler.text;
                    Debug.Log($"[GameSetting][Response] ��ȡ GameSetting �ɹ� {responseText}");
                    gameSetting = JsonUtility.FromJson<GameSetting>(responseText);
                }

                if (gameSetting.envVersion == "tf") {
                    gameSetting.serverHost += tfHostSuffix;
                }
                else {
                    gameSetting.serverHost += devHostSuffix;
                }

#if UNITY_EDITOR
                if (isDebugHttp) {
                    gameSetting.serverHost = wxLoginServeHostDebug;
                }
                else {
                    gameSetting.serverHost = wxLoginServerHostDev;
                }
#endif

                if (CheckIsWhiteList()) {
                    isGameSettingLoadCompleted = false;
                    GameSettingInit(true);
                }
                else {
                    isGameSettingInitCompleted = true;
                    // ReqGameMaintenanceInfo(null);
                    OnApplicationBasicInitComplete();
                }
            }
        }
    }

    // ����Ƿ��ǰ�����
    bool CheckIsWhiteList() {
        if (!gameSetting.openWhiteList) {
            return false;
        }

        if (ipAddress == string.Empty) {
            return false;
        }

        if (gameSetting.whiteList == null) {
            return false;
        }

        for (int i = 0; i < gameSetting.whiteList.Length; i++) {
            if (ipAddress == gameSetting.whiteList[i]) {
                Debug.Log($"[GameSetting][WhiteCheck] ��ǰ IP λ�ڰ�����");
                return true;
            }
        }

        return false;
    }

    #endregion

    #region IP

    // ��ȡ���� IP ��ַ
    private void GetIPAddress() {
        string url = getIPAddressServe;
        ipAddressWebRequest = UnityWebRequest.Get(url);
        ipAddressWebRequest.SendWebRequest();
    }

    // IP ��ַ Tick
    void IPAddresTick() {
        if (ipAddressWebRequest != null && ipAddressWebRequest.isDone) {
            if (ipAddressWebRequest.result != UnityWebRequest.Result.Success) {
                // TODO: UI ��ʾ
                Debug.Log($"[GameSetting][IP] ��ȡIPʧ�� {ipAddressWebRequest.error}");
            }
            else {
                Debug.Log($"[GameSetting][IP] ��ȡIP�ɹ� {ipAddressWebRequest.downloadHandler.text}");
                IPMessage iPMessage = new IPMessage();
                iPMessage.ToData(ipAddressWebRequest.downloadHandler.text);
                ipAddress = iPMessage.Ip;
                Debug.Log($"[GameSetting][IP] ����IP��ַ��{ipAddress}");
            }

            GameSettingInit(false);
            ipAddressWebRequest = null;
        }
    }

    #endregion


    // ������ʼ����ɼ�⣬���Խ��к�������ϵͳ��ʼ��
    void OnApplicationBasicInitComplete() {
        // �Ƿ�ɹ��õ� wxcode
        if (!isWxSdkLoginInitCompleted)
            return;
        // �Ƿ��ȡ�� cdn gamesetting
        if (!isGameSettingInitCompleted)
            return;

        resHelper.Init();

        //ResHelper.Preload(new string[] {
        //    ResKey.gameMgr,
        //    ResKey.confs
        //}, () => {
        //    GameConf.Init();
        //    GameObject gameMgrObj = Instantiate(ResHelper.GetAsset<GameObject>(ResKey.gameMgr));
        //    DontDestroyOnLoad(gameMgrObj);
        //    LoginMgr.instance.EnterGame();
        //});

        ResHelper.db.LoadScene(ResKey.MainLevelScene, LoadSceneMode.Single);

        Debug.Log("[GameSetting] OnApplicationBasicInitComplete");
    }
}
