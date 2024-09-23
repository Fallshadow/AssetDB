using FallShadow.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {

    public struct UAsset { }

    public class AssetDB {

        public enum LoadMode {
            Editor,
            Runtime
        }
        private LoadMode loadMode;

        private bool needInvokeGlobFiles;

        // 本地资源加载路径   mount："d:/app/sandbox"
        // 文件加载键      filekey: "foo/bar/foobar.bundle"
        // 本地资源最终路径filepath: "d:/app/sandbox/foo/bar/foobar.bundle"
        private NativeList<FixedString512Bytes> mounts;
        private Dictionary<FixedString512Bytes, string> fileKey2FilePath;


        // 文件扩展名对应资源类型 extension to AssetType
        // 比如 .prefab 对应 GameObject
        private static Dictionary<FixedString32Bytes, Type> ext2AssetType;
        // 为二进制文件单独创建一个列表储存这些扩展名
        private static NativeList<FixedString32Bytes> binaryAssetExts;

        // TODO

        private int bundleRequestTaskCount;
        private BundleRequestTask[] bundleRequestTasks;
        
        private BundleTask[] bundleTasks;
        private int bundleTaskCount;
        private Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Deps;
        private int requestAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAssetTasks;
        private int requestAllAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAllAssetTasks;
        private AssetTask[] assetTasks;
        private int assetTaskCount;
        private AssetCache[] assetCaches;
        private int assetCacheCount;
        private SceneTask[] sceneTasks;
        private int sceneTaskCount;
        private NativeHashMap<FixedString512Bytes, SceneInfo> url2SceneInfo;
        private Dictionary<int, FixedString32Bytes> handle2SceneName;
        private ushort releaseBundleCounter;
        private NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes> subSceneFileName2Path;
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> url2ContentCatalogPath;
        // 加载远程或本地相对与 StreamingAssets 下的资源
        private RequestFileTask[] requestFileTasks;
        internal int requestFileTaskCount;
        private NativeList<FixedString512Bytes> specialFilePaths;
        // examples: "https://s3.sofunny.io/forbar/v1.0.1/StreamingAssets"
        private FixedString512Bytes specialDirectory;
        internal HandleManager<UAsset> handleManager;
        internal NativeHashMap<FixedString512Bytes, Handle<UAsset>> url2handle;
        // example 1:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        private NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Assets;
        private NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>> handle2AllAssetHandles;
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> hash2normal;
        // index: handle.index, value: refCount
        internal NativeArray<int> refCounts;
        // example:
        // key: new Handle<UAsset>(1, 1).index
        // Value: AssetBundle
        internal Dictionary<int, AssetBundle> index2Bundle;

        private const int maxMountCount = 16;
        private const int maxBundleRequesTaskCount = 256;

        // TODO
        private const int prefAssetTypeCount = 16;
        private const int maxBundleTaskCount = 256;
        private const int maxAllAssetTaskCount = 32;
        private const int maxAssetTaskCount = 256;
        private const int maxSceneTaskCount = 32;
        private const int maxSubSceneFileCount = 128;
        private const int maxSubSceneCatalogCount = 16;
        private const int maxFileTaskCount = 4;
        private const int maxAssetCount = 4096;

#if UNITY_EDITOR
        private struct RequestEditorAssetTask {
            public Handle<UAsset> handle;
            public FixedString512Bytes url;
        }

        private int requestEditorAssetTaskCount;
        private NativeArray<RequestEditorAssetTask> requestEditorAssetTasks;

        private struct RequestEditorSceneTask {
            public Handle<UAsset> handle;
            public Scene scene;
        }

        private int requestEditorSceneTaskCount;
        private NativeArray<RequestEditorSceneTask> requestEditorSceneTasks;
#endif

        public void Initialize(LoadMode mode = LoadMode.Runtime) {
            loadMode = mode;
            if(loadMode == LoadMode.Editor) {
                Debug.LogWarning("[AssetDB][Initialize] 编辑器模式，开启 AssetDatabase 模拟加载");
            }

            needInvokeGlobFiles = false;

            mounts = new NativeList<FixedString512Bytes>(maxMountCount, Allocator.Persistent);
            fileKey2FilePath = new Dictionary<FixedString512Bytes, string>();

            ext2AssetType = new Dictionary<FixedString32Bytes, Type>(prefAssetTypeCount);
            binaryAssetExts = new NativeList<FixedString32Bytes>(prefAssetTypeCount, Allocator.Persistent);

            // bundle
            bundleRequestTasks = new BundleRequestTask[maxBundleRequesTaskCount];
            bundleTasks = new BundleTask[maxBundleTaskCount];
            bundleKey2Deps = new Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>>();

            // load
            requestAssetTasks = new NativeArray<RequestAssetTask>(maxAssetTaskCount, Allocator.Persistent);
            assetTasks = new AssetTask[maxAssetTaskCount];

            // load all
            requestAllAssetTasks = new NativeArray<RequestAssetTask>(maxAllAssetTaskCount, Allocator.Persistent);

            // load scene
            sceneTasks = new SceneTask[maxSceneTaskCount];
            url2SceneInfo = new NativeHashMap<FixedString512Bytes, SceneInfo>(maxSceneTaskCount, Allocator.Persistent);
            handle2SceneName = new Dictionary<int, FixedString32Bytes>();

            // sub scene
            subSceneFileName2Path = new NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes>(maxSubSceneFileCount, Allocator.Persistent);
            url2ContentCatalogPath = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxSubSceneCatalogCount, Allocator.Persistent);

            // files
            requestFileTasks = new RequestFileTask[maxFileTaskCount];
            specialFilePaths = new NativeList<FixedString512Bytes>(maxFileTaskCount, Allocator.Persistent);
            specialDirectory = Application.streamingAssetsPath.Replace("\\", sep.ToString());

            // asset
            handleManager = new HandleManager<UAsset>(maxAssetCount);
            url2handle = new NativeHashMap<FixedString512Bytes, Handle<UAsset>>(maxAssetCount, Allocator.Persistent);
            url2AssetInfo = new NativeHashMap<FixedString512Bytes, AssetInfo>(maxAssetCount, Allocator.Persistent);
            bundleKey2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(maxAssetCount, Allocator.Persistent);
            handle2AllAssetHandles = new NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>>(maxAssetCount, Allocator.Persistent);
            hash2normal = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxAssetCount, Allocator.Persistent);
            refCounts = new NativeArray<int>(maxAssetCount, Allocator.Persistent);
            index2Bundle = new Dictionary<int, AssetBundle>();
            assetCaches = new AssetCache[maxAssetCount];

            bundleRequestTaskCount = 0;
            bundleTaskCount = 0;
            requestAllAssetTaskCount = 0;
            requestAssetTaskCount = 0;
            assetTaskCount = 0;
            assetCacheCount = 0;
            sceneTaskCount = 0;
            requestFileTaskCount = 0;
            releaseBundleCounter = 0;

#if UNITY_EDITOR
            requestEditorAssetTasks = new NativeArray<RequestEditorAssetTask>(maxAssetCount, Allocator.Persistent);
            requestEditorAssetTaskCount = 0;
            requestEditorSceneTasks = new NativeArray<RequestEditorSceneTask>(maxSceneTaskCount, Allocator.Persistent);
            requestEditorSceneTaskCount = 0;
#endif
        }
        
        public void RegisterAsset(FixedString32Bytes extension, Type type, bool isBinary) {
            ext2AssetType[extension] = type;

            if(isBinary) {
                if(!binaryAssetExts.Contains(extension)) {
                    binaryAssetExts.Add(extension);
                }
            }
        }

        // TODO
        public enum TaskType {
            Bundle,
            BundleAsset,
            Asset,
            None
        }

        private struct RequestAssetTask {
            public Handle<UAsset> handle;
            public FixedString512Bytes url;
        }

        private struct SceneInfo {
            public FixedString32Bytes sceneName;
            public LoadSceneMode loadSceneMode;
        }

        private struct SceneTask {
            public Handle<UAsset> handle;
            public SceneInfo sceneInfo;
            public FixedString512Bytes bundleUrl;
            public bool catalogIsLoaded;
            public AsyncOperation asyncOperation;
        }

        private struct RequestFileTask {
            public FileType type;
            public FixedString512Bytes url;
            public UnityWebRequestAsyncOperation operation;
        }

        internal struct AssetInfo {
            public TaskType type;
            public FixedString512Bytes bundleKey;
            public FixedString512Bytes assetPath;
        }

        private static readonly FixedString32Bytes sep = "/";

    }

}

