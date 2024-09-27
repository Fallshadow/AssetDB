using UnityEngine;

internal static class LoginRequest {
    public static void SendGameLogin(string token) {
        //LoginPb loginInfo = new LoginPb() {
        //    loginInfo.token = token;
        //    loginInfo.version = "v0.8.0.240819";
        //    loginInfo.close_heartbeat = true;
        //    loginInfo.avatar_url = PlayerManager.instance.avatar_url;
        //}

        //NetManager.instance.WsSend(NetEvent.Login_Request, loginInfo);
    }
}