using System;

[Serializable]
public class GameSetting {
    public string versionName;
    public int buildNum;
    public int resNum;
    public int runtimeNum;
    public string serverHost;
    public bool openDebugConsole;
    public bool openWhiteList;
    public string[] whiteList;
    public string gameMaintenanceUrl; // ��Ϸά����ַ
    public bool isWXSDKTokenLogin;
    public bool isOpenGravitySDK; // ������� SDK
    public bool isOpenFunnyDBSDK; // ҵ���� SDK
    public bool isAndroidOpenPaySDK; // ��׿ ΢��֧��SDK
    public bool isIOSOpenPaySDK; // IOS ΢��֧��SDK
    public bool isPCOpenPaySDK; // PC ΢��֧��SDK
    public string envVersion; // ��Ϸ���� develop ������  tf ���� release ������
}