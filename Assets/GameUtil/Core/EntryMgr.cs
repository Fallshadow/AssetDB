using UnityEngine;
using UnityEngine.SceneManagement;

// ������Ϸǰ��һЩ����
// ������ɺ������Ϸ
public class EntryMgr : MonoBehaviour
{
    public ResHelper resHelper;

    void Start()
    {
        resHelper.Init();
        ResHelper.db.LoadScene(ResKey.MainLevelScene, LoadSceneMode.Single);
    }
}
