using FallShadow.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Networking;
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

        private BundleRequestTask[] bundleRequestTasks;

        // TODO
        private static Dictionary<FixedString32Bytes, Type> ext2AssetType;
        private static NativeList<FixedString32Bytes> binaryAssetExts;
        private BundleTask[] bundleTasks;
        private Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Deps;
        private int requestAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAssetTasks;
        private int requestAllAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAllAssetTasks;
        private AssetTask[] assetTasks;
        private SceneTask[] sceneTasks;
        private int sceneTaskCount;
        private NativeHashMap<FixedString512Bytes, SceneInfo> url2SceneInfo;
        private Dictionary<int, FixedString32Bytes> handle2SceneName;
        // private NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes> subSceneFileName2Path;
        // private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> url2ContentCatalogPath;
        // 加载远程或本地相对与 StreamingAssets 下的资源
        private RequestFileTask[] requestFileTasks;
        private NativeList<FixedString512Bytes> specialFilePaths;
        // examples: "https://s3.sofunny.io/forbar/v1.0.1/StreamingAssets"
        private FixedString512Bytes specialDirectory;

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
            // subSceneFileName2Path = new NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes>(maxSubSceneFileCount, Allocator.Persistent);
            // url2ContentCatalogPath = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxSubSceneCatalogCount, Allocator.Persistent);

            // files
            requestFileTasks = new RequestFileTask[maxFileTaskCount];
            specialFilePaths = new NativeList<FixedString512Bytes>(maxFileTaskCount, Allocator.Persistent);
            specialDirectory = Application.streamingAssetsPath.Replace("\\", sep.ToString());
        }

        // TODO
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

        private static readonly FixedString32Bytes sep = "/";

    }

}

