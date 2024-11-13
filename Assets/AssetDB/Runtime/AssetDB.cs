using FallShadow.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {

    public struct UAsset { }


    // 首先需要理解资源路径资源名称
    // Unity 环境下，资源放置在 Sandbox 下，file.txt 为资源清单。
    // 使用方法：files 文件清单先设置，然后设置标记，决定是否直接读取所有资源
    public partial class AssetDB {

        public enum LoadMode {
            Editor,
            Runtime
        }
        private LoadMode loadMode;











        // 文件扩展名对应资源类型 extension to AssetType
        // 比如 .prefab 对应 GameObject
        private static Dictionary<FixedString32Bytes, Type> ext2AssetType;
        // 为二进制文件单独创建一个列表额外储存这些扩展名
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




        // examples: "https://s3.sofunny.io/forbar/v1.0.1/StreamingAssets"

        internal HandleManager<UAsset> handleManager;

        

        private NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>> handle2AllAssetHandles;

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

        private const int maxAssetCount = 4096;



        public void Initialize(bool needLoadGlobFiles = true, LoadMode mode = LoadMode.Runtime) {
            loadMode = mode;
            if (loadMode == LoadMode.Editor) {
                Debug.LogWarning("[AssetDB][Initialize] 编辑器模式，开启 AssetDatabase 模拟加载");
            }
            else if (loadMode == LoadMode.Runtime) {
                Debug.LogWarning("[AssetDB][Initialize] 运行模式，直接读取资源");
            }

            this.needLoadGlobFiles = needLoadGlobFiles;

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

            InitEcsContainer();

            // files
            InitFileTask();

            // asset
            handleManager = new HandleManager<UAsset>(maxAssetCount);
            url2handle = new NativeHashMap<FixedString512Bytes, Handle<UAsset>>(maxAssetCount, Allocator.Persistent);
            url2AssetInfo = new NativeHashMap<FixedString512Bytes, AssetInfo>(maxAssetCount, Allocator.Persistent);
            bundleKey2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(maxAssetCount, Allocator.Persistent);
            handle2AllAssetHandles = new NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>>(maxAssetCount, Allocator.Persistent);
            
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

            if (isBinary) {
                if (!binaryAssetExts.Contains(extension)) {
                    binaryAssetExts.Add(extension);
                }
            }
        }


        public void Dispose() {
            if (bundleKey2Deps != null) {
                foreach (var dependencies in bundleKey2Deps.Values) {
                    if (dependencies.IsCreated) {
                        dependencies.Dispose();
                    }
                }

                bundleKey2Deps.Clear();
                bundleKey2Deps = null;
            }

            if (url2AssetInfo.IsCreated) {
                url2AssetInfo.Dispose();
            }

            if (bundleKey2Assets.IsCreated) {
                foreach (var kv in bundleKey2Assets) {
                    if (kv.Value.IsCreated) {
                        kv.Value.Dispose();
                    }
                }

                bundleKey2Assets.Dispose();
            }

            if (mounts.IsCreated) {
                mounts.Dispose();
            }

            ext2AssetType.Clear();
            ext2AssetType = null;

            if (binaryAssetExts.IsCreated) {
                binaryAssetExts.Dispose();
            }

            if (fileKey2FilePath != null) {
                fileKey2FilePath.Clear();
                fileKey2FilePath = null;
            }

            depsTasks = null;

            if (requestAllAssetTasks.IsCreated) {
                requestAllAssetTasks.Dispose();
            }

            if (requestAssetTasks.IsCreated) {
                requestAssetTasks.Dispose();
            }

            bundleTasks = null;
            sceneTasks = null;
            



            if (index2Bundle != null) {
                foreach (var bundle in index2Bundle.Values) {
                    if (bundle != null) {
                        bundle.Unload(true);
                    }
                }
            }

            index2Bundle = null;
            handle2SceneName = null;

            if (handle2AllAssetHandles.IsCreated) {
                foreach (var kv in handle2AllAssetHandles) {
                    if (kv.Value.IsCreated) {
                        kv.Value.Dispose();
                    }
                }

                handle2AllAssetHandles.Dispose();
            }


            if (url2SceneInfo.IsCreated) {
                url2SceneInfo.Dispose();
            }


            if (handleManager.IsCreated()) {
                handleManager.Dispose();
            }

            if (url2handle.IsCreated) {
                url2handle.Dispose();
            }

            if (refCounts.IsCreated) {
                refCounts.Dispose();
            }

            ReleaseFileTask();
            ReleaseEcsContainer();

#if UNITY_EDITOR
            if (requestEditorAssetTasks.IsCreated) {
                requestEditorAssetTasks.Dispose();
            }

            if (requestEditorSceneTasks.IsCreated) {
                requestEditorSceneTasks.Dispose();
            }
#endif
        }


        public void Tick() {
            TickRequestFileTasks();
#if UNITY_EDITOR
            TickRequestEditorAssetTasks();
            TickRequestEditorSceneTasks();
#endif
            TickRequestAllAssetTasks();
            TickRequestAssetTasks();
            TickDepsTasks();
            TickBundleTasks();
            TickAssetTasks();
            TickSceneTasks();
            // AutoReleaseBundle();
        }





        



        private void TickRequestAllAssetTasks() {
            if (requestFileTaskCount > 0) {
                return;
            }

            for (int t = 0; t < requestAllAssetTaskCount; t++) {
                RequestAssetTask requestAllAssetTask = requestAllAssetTasks[t];

                if (!handleManager.IsValid(requestAllAssetTask.handle)) {
                    requestAllAssetTaskConsumeAt(ref t);
                    continue;
                }

                var bundleKey = GetBundleKey(requestAllAssetTask.url);

                if (!bundleKey2Assets.TryGetValue(bundleKey, out var assets)) {
                    requestAllAssetTaskConsumeAt(ref t);
                    continue;
                }

                // 记录 allHandle 与它持有的 allAssetHandles 的映射
                if (!handle2AllAssetHandles.TryGetValue(requestAllAssetTask.handle, out var allAssetHandles)) {
                    allAssetHandles = new NativeList<Handle<UAsset>>(16, Allocator.Persistent);
                    handle2AllAssetHandles[requestAllAssetTask.handle] = allAssetHandles;
                }

                foreach (var assetUrl in assets) {
                    //  assetUrl 对应的 handle 不存在则创建加载任务，存在则引用计数 ++
                    if (!url2handle.TryGetValue(assetUrl, out var handle)) {
                        handle = CreateRequestAssetTask(assetUrl);
                    }
                    else {
                        refCounts[handle.index]++;
                    }

                    allAssetHandles.Add(handle);
                }

                requestAllAssetTaskConsumeAt(ref t);
            }
        }

        private void TickRequestAssetTasks() {
            if (requestFileTaskCount > 0) {
                return;
            }

            for (int t = 0; t < requestAssetTaskCount; t++) {
                RequestAssetTask requestAssetTask = requestAssetTasks[t];

                if (!handleManager.IsValid(requestAssetTask.handle)) {
                    requestAssetTaskConsumeAt(ref t);
                    continue;
                }

                var taskType = GetTaskType(requestAssetTask.url);

                switch (taskType) {
                    case TaskType.None:
                        requestAssetTaskConsumeAt(ref t);
                        continue;
                    case TaskType.Bundle:
                    case TaskType.BundleAsset:
                        var bundleKey = GetBundleKey(requestAssetTask.url);

                        if (!RequestBundleTask(bundleKey)) {
                            Release(requestAssetTask.handle);
                            // 不创建 assetTasks
                            requestAssetTaskConsumeAt(ref t);
                            continue;
                        }

                        break;
                }
            }
        }

        



        private void TickDepsTasks() {
            for (var t = 0; t < depsTaskCount; t++) {
                ref var task = ref depsTasks[t];

                if (bundleKey2Deps.ContainsKey(task.bundleKey)) {
                    requestDepsTaskConsumeAt(ref t);
                    continue;
                }

                if (task.webOperation == null) {
                    var filePath = GetDepsFilePath(task.bundleKey);

                    if (string.IsNullOrEmpty(filePath)) {
                        bundleKey2Deps[task.bundleKey] = new NativeList<FixedString512Bytes>(0, Allocator.Persistent);
                        requestDepsTaskConsumeAt(ref t);
                        continue;
                    }

                    var request = UnityWebRequest.Get(filePath);
                    task.webOperation = request.SendWebRequest();
                }

                if (task.webOperation.isDone && task.webOperation.webRequest.isDone) {
                    if (string.IsNullOrEmpty(task.webOperation.webRequest.error)) {
                        var bytes = task.webOperation.webRequest.downloadHandler.data;
                        AddDeps(task.bundleKey, bytes);
                    }

                    task.webOperation = null;
                    requestDepsTaskConsumeAt(ref t);
                }
            }
        }

        // bundleKey: "foo/bar/foobar.bundle"
        // fileKey: "foo/bar/foobar.deps"
        // filePath: fileKey2FilePath[fileKey]
        private string GetDepsFilePath(FixedString512Bytes bundleKey) {
            var index = bundleKey.IndexOf(bundleSep);

            if (index == -1) {
                return null;
            }

            var fileKey = FixedStringUtil.Substring(bundleKey, 0, index);
            fileKey.Append(depsSep);

            if (fileKey2FilePath.TryGetValue(fileKey, out var path)) {
                return path;
            }

            return null;
        }

        private void AddDeps(FixedString512Bytes bundleKey, byte[] bytes) {
            if (bytes == null || bytes.Length == 0) {
                return;
            }

            unsafe {
                var ptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0).ToPointer();
                var cursor = 0;
                var length = bytes.Length;
                NativeList<FixedString512Bytes> deps;

                if (bundleKey2Deps.ContainsKey(bundleKey)) {
                    deps = bundleKey2Deps[bundleKey];
                }
                else {
                    deps = new NativeList<FixedString512Bytes>(16, Allocator.Persistent);
                    bundleKey2Deps[bundleKey] = deps;
                }

                for (var i = 0; i < length; i++) {
                    var item = ptr[i];

                    if (item == '\n') {
                        var dep = FixedStringUtil.GetFixedString512(ptr, cursor, i - cursor);

                        if (dep[dep.Length - 1] == '\r') {
                            dep.Length -= 1;
                        }

                        deps.Add(dep);
                        cursor = i + 1;
                    }
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

        private enum FileType {
            Files
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



        internal struct AssetInfo {
            public TaskType type;
            public FixedString512Bytes bundleKey;
            public FixedString512Bytes assetPath;
        }

        public enum Status {
            Invalid,
            Loading,
            Succeeded,
            Failed
        }

    }

}

