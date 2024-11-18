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
    public string gameMaintenanceUrl; // 游戏维护地址
    public bool isWXSDKTokenLogin;
    public bool isOpenGravitySDK; // 引力打点 SDK
    public bool isOpenFunnyDBSDK; // 业务打点 SDK
    public bool isAndroidOpenPaySDK; // 安卓 微信支付SDK
    public bool isIOSOpenPaySDK; // IOS 微信支付SDK
    public bool isPCOpenPaySDK; // PC 微信支付SDK
    public string envVersion; // 游戏环境 develop 开发版  tf 提审 release 发布版
}