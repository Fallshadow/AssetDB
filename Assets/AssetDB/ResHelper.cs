using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FallShadow.Asset.Runtime;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;
using static UnityEditor.VersionControl.Asset;
using System.Xml;
using System;
using FallShadow.Common;

public class ResHelper : MonoBehaviour {
    public static ResHelper instance { get; private set; }
    public static AssetDB db { get; internal set; }

    [SerializeField]
    private RemoteConfig remoteConfig;

    private bool isInited = false;
    private string sandboxMountPath = $"{Application.dataPath}/../sandbox";
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

    private void Update() {
        if (!isInited) { return; }

        db?.Tick();

        checkStateLoad();
        checkPreload();
    }

    private void OnDestroy() {
        db?.UnMount(sandboxMountPath);
        db?.Dispose();
        db = null;
    }

    public void Init() {
        db = new AssetDB();
        db.Initialize(loadMode);

        db.RegisterAsset(".txt", typeof(TextAsset), false);
        db.RegisterAsset(".toml", typeof(TextAsset), false);
        db.RegisterAsset(".xml", typeof(TextAsset), false);
        db.RegisterAsset(".level", typeof(TextAsset), false);
        db.RegisterAsset(".bin", typeof(TextAsset), true);
        db.RegisterAsset(".dll", typeof(TextAsset), true);
        db.RegisterAsset(".prefab", typeof(GameObject), false);
        db.RegisterAsset(".mat", typeof(Material), false);
        db.RegisterAsset(".shader", typeof(Shader), false);
        db.RegisterAsset(".anim", typeof(AnimationClip), false);
        db.RegisterAsset(".controller", typeof(RuntimeAnimatorController), false);
        db.RegisterAsset(".sprite", typeof(Sprite), false);
        db.RegisterAsset(".spriteatlas", typeof(SpriteAtlas), false);
        db.RegisterAsset(".asset", typeof(ScriptableObject), false);
        db.RegisterAsset(".png", typeof(Texture), false);
        db.RegisterAsset(".jpg", typeof(Texture), false);
        db.RegisterAsset(".tga", typeof(Texture), false);
        db.RegisterAsset(".unity", typeof(Scene), false);

#if WECHAT && !UNITY_EDITOR
        db.SetSpecialDirectory($"{remoteConfig.remoteUrl}/StreamingAssets");
        db.SetFilesUrl($"{remoteConfig.remoteUrl}/StreamingAssets/files.txt");
        db.Mount($"{remoteConfig.remoteUrl}/StreamingAssets");
#else
        db.SetFilesUrl($"{sandboxMountPath}/ files.txt");
        db.Mount(sandboxMountPath);
#endif
        db.GlobFiles();

        isInited = true;
    }

    #region Load

    private enum LoadState {
        None,
        Loading,
        Loaded
    }

    private class AssetState {
        public string url;
        public List<Action<UnityEngine.Object>> callbacks;

        public LoadState state = LoadState.None;
        public Handle<UAsset> handle;
    }

    // Assets 下面的相对路径加上协议头 asset://，比如 "asset://Prefabs/GameMgr.prefab" 对应的 资源信息
    private Dictionary<string, AssetState> states = new();
    private List<Action<UnityEngine.Object>> TempCallList = new();

    public static T GetAssetFromCache<T>(string url) where T : UnityEngine.Object
        => instance.DoGetAssetFromCache<T>(url);

    private T DoGetAssetFromCache<T>(string url) where T : UnityEngine.Object {
        string url_ = "asset://" + url;
        if (states.TryGetValue(url_, out AssetState state) && state.state == LoadState.Loaded) {
            return db.GetAssetFromCache(state.handle) as T;
        }
        else {
            throw new Exception("asset is not loaded: " + url_);
        }
    }

    public static void LoadAsset<T>(string url, Action<T> callback) where T : UnityEngine.Object
        => instance.doLoadAsset(url, callback);

    private void doLoadAsset<T>(string url, Action<T> callback) where T : UnityEngine.Object {
        AssetState state = CreateLoad(url, obj => {
            callback?.Invoke(obj as T);
        });
    }

    private AssetState CreateLoad(string url, Action<UnityEngine.Object> callback) {
        string _url = "asset://" + url;
        if (!states.TryGetValue(_url, out AssetState state)) {
            state = new AssetState {
                url = _url,
                callbacks = new List<Action<UnityEngine.Object>>()
            };
            states.Add(_url, state);
        }
        state.callbacks.Add(callback);

        if (state.state == LoadState.None) {
            state.handle = db.Load(_url);
            state.state = LoadState.Loading;
        }

        return state;
    }

    private void checkStateLoad() {
        foreach (AssetState state in states.Values) {

            if (state.state == LoadState.Loading && db.IsSucceeded(state.handle)) {
                state.state = LoadState.Loaded;
                Debug.LogWarning(state.url + " loaded");
            }

            if (state.state == LoadState.Loaded) {
                TempCallList.AddRange(state.callbacks);
                state.callbacks.Clear();
            }

            UnityEngine.Object obj = db.GetAssetFromCache(state.handle);
            foreach (Action<UnityEngine.Object> callback in TempCallList) {
                //Debug.Log("111");
                callback?.Invoke(obj);
            }

            TempCallList.Clear();
        }
    }

    #endregion

    #region PreLoad

    private class PreloadGroup {
        public int totalCount;
        public int count;
        public Action endCallback;
    }

    private List<PreloadGroup> preloadList = new();
    private HashSet<string> urlUniqueSet = new();

    public static void Preload(IList<string> urls, Action callback_)
        => instance.doPreload(urls, callback_);

    private void doPreload(IList<string> urls, Action callback_) {

        urlUniqueSet.Clear();
        foreach (string url in urls) {
            urlUniqueSet.Add(url);
        }

        PreloadGroup group = new() {
            endCallback = callback_,
            totalCount = urlUniqueSet.Count,
            count = 0
        };

        foreach (string url in urlUniqueSet) {
            CreateLoad(url, obj => {
                group.count++;
            });
        }

        preloadList.Add(group);
        urlUniqueSet.Clear();
    }

    private void checkPreload() {
        for (int i = 0; i < preloadList.Count; i++) {
            PreloadGroup group = preloadList[i];
            // Debug.Log("[ResHelper]" + group.count + " / " + group.totalCount);
            if (group.count == group.totalCount) {
                preloadList.RemoveAt(i);
                group.endCallback?.Invoke();
            }
        }
    }
    #endregion
}
