using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : UIForm {
    public Button loginBtn;

    public override void OnInit() {
        base.OnInit();

        loginBtn.onClick.AddListener(() => {
            ResHelper.db.LoadScene(ResKey.MainLevelScene, LoadSceneMode.Single);
            GameMgr.ui.Close(this); 
        });
    }
}