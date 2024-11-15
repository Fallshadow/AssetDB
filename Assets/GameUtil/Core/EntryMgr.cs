using UnityEngine;
using UnityEngine.SceneManagement;

// 进入游戏前的一些设置
// 设置完成后进入游戏
public class EntryMgr : MonoBehaviour
{
    public ResHelper resHelper;

    void Start()
    {
        resHelper.Init();
        ResHelper.db.LoadScene(ResKey.MainLevelScene, LoadSceneMode.Single);
    }
}
