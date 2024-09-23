using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallShadow.Asset.Runtime;


public class ResHelper : MonoBehaviour {
    public static ResHelper instance { get; private set; }
    public static AssetDB db { get; internal set; }

    private AssetDB.LoadMode loadMode {
#if UNITY_EDITOR
        get => PlayerPrefs.GetInt(AssetDBKey.AssetDatabasePrefKey) == 1 ? AssetDB.LoadMode.Editor : AssetDB.LoadMode.Runtime;
#else
        get => AssetDB.LoadMode.Runtime;
#endif
    }

    private void Awake() {
        DontDestroyOnLoad(this);
        instance = this;
    }

    public void Init() {
        db = new AssetDB();
        db.Initialize(loadMode);
    }

}
