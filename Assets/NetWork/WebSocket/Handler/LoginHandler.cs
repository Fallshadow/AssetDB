public class LoginHandler : BaseHandler {
    protected override void initEvents() {
        NetManager.instance.WsListen(NetEvent.Login_Response, onLogin);
    }

    protected override void releaseEvents() {
        NetManager.instance.WsRemoveListen(NetEvent.Login_Response, onLogin);
    }

    private void onLogin(object packet) {
        // pb p = (pb)packet;


    }
}