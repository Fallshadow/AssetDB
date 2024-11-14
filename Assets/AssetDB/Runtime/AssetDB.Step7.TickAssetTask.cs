using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {

    public partial class AssetDB {
        public struct AssetCache {
            public Handle<UAsset> handle;
            public FixedString512Bytes url;
            public bool succeed;
            public UnityEngine.Object asset;
            public string text;
            public byte[] bytes;
            public int frame;
        }

        private int assetTaskCount;
        private AssetTask[] assetTasks;

        private int assetCacheCount;
        private AssetCache[] assetCaches;

        private const int maxAssetTaskCount = 256;

        private void InitAssetTask() {
            assetTaskCount = 0;
            assetCacheCount = 0;

            assetTasks = new AssetTask[maxAssetTaskCount];
            assetCaches = new AssetCache[maxAssetCount];
        }
        
        private void DisposeAssetTask() {

        }

        public UnityEngine.Object GetAssetFromCache(Handle<UAsset> handle) {
            if (!handleManager.IsValid(handle)) {
                return null;
            }

            for (var i = 0; i < assetCacheCount; i++) {
                ref var cache = ref assetCaches[i];

                if (cache.handle.Equals(handle)) {
                    return cache.asset;
                }
            }

            return null;
        }

        private void TickAssetTasks() {
            for (var i = 0; i < assetTaskCount; i++) {
                ref var task = ref assetTasks[i];
                var taskType = GetTaskType(task.url);

                switch (taskType) {
                    case TaskType.BundleAsset: {
                            var bundleKey = GetBundleKey(task.url);
                            var bundleUrl = $"{protocolSep}{bundleKey}";

                            if (!url2handle.TryGetValue(bundleUrl, out var handle)) {
                                continue;
                            }

                            if (!index2Bundle.TryGetValue(handle.index, out var bundle)) {
                                continue;
                            }

                            if (task.request == null) {
                                var (assetPath, assetType) = GetBundleAssetPathAndType(task.url);
                                var isScene = task.url.IndexOf(sceneSep) != -1;

                                if (isScene) {
                                    // 场景资源不存入 assetCaches，且直接创建场景加载任务
                                    task.isDone = true;

                                    if (url2SceneInfo.TryGetValue(task.url, out var sceneInfo)) {
                                        // 由于加载协议全小写，从 bundle 中获取真实的场景名
                                        var index = task.url.LastIndexOf(sep);
                                        var sceneWithExt = FixedStringUtil.Substring(task.url, index + 1).ToString();
                                        FixedString32Bytes sceneName = default;

                                        foreach (var scenePath in bundle.GetAllScenePaths()) {
                                            var idx = scenePath.ToLower().IndexOf(sceneWithExt);

                                            if (idx != -1) {
                                                sceneName = scenePath.Substring(idx, scenePath.Length - idx - sceneSep.Length);
                                                break;
                                            }
                                        }

                                        if (sceneName.Length == 0) {
                                            throw new Exception($"not found scene name from {bundleUrl}");
                                        }

                                        sceneInfo.sceneName = sceneName;
                                        sceneTasks[sceneTaskCount++] = new SceneTask {
                                            handle = task.handle,
                                            sceneInfo = sceneInfo,
                                            catalogIsLoaded = false,
                                            bundleUrl = bundleUrl
                                        };
                                    }
                                }
                                else if (assetType != null) {
                                    task.request = bundle.LoadAssetAsync(assetPath.ToString(), assetType);
                                }
                                else {
                                    task.isDone = true;
                                }
                            }

                            if (!task.isDone) {
                                if (task.request.isDone) {
                                    CacheAsset(task);
                                    task.isDone = true;
                                }
                            }
                        }
                        break;
                    case TaskType.Asset: {
                            if (task.webOperation == null) {
                                var request = UnityWebRequest.Get(GetAssetFilePath(task.url));
                                task.webOperation = request.SendWebRequest();
                            }

                            if (task.delayFrame > 0) {
                                task.delayFrame--;
                                assetTasks[i] = task;
                                continue;
                            }

                            if (task.delaySeconds > 0) {
                                task.delaySeconds -= Time.deltaTime;
                                assetTasks[i] = task;
                                continue;
                            }

                            if (task.webOperation.isDone && task.webOperation.webRequest.isDone) {
                                if (string.IsNullOrEmpty(task.webOperation.webRequest.error)) {
                                    CacheAsset(task);
                                }
                                else {
                                    Debug.LogError($"[AssetDB] 加载资源: {task.url} 失败! error: {task.webOperation.webRequest.error}");
                                }

                                task.isDone = true;
                            }
                        }
                        break;

                    case TaskType.None:
                        task.isDone = true;
                        break;
                }

                if (task.isDone) {
                    assetTaskConsumeAt(ref i);
                }
            }
        }

        private void CacheAsset(AssetTask task) {
            if (!handleManager.IsValid(task.handle)) {
                return;
            }

            var cache = new AssetCache {
                handle = task.handle,
                url = task.url,
                frame = Time.frameCount,
                succeed = false
            };

            var taskType = GetTaskType(task.url);

            // 更新 BundleAsset 所属 bundle 的引用计数
            if (taskType.Equals(TaskType.BundleAsset)) {
                var bundleKey = GetBundleKey(task.url);
                var bundleUrl = $"{protocolSep}{bundleKey}";
                var handle = url2handle[bundleUrl];
                refCounts[handle.index]++;
            }

            if (task.request != null) {
                if (task.request.asset != null) {
                    if (task.request.asset is TextAsset textAsset) {
                        if (IsBinaryFile(task.url)) {
                            cache.bytes = textAsset.bytes;
                        }
                        else {
                            cache.text = textAsset.text;
                        }
                    }
                    else {
                        cache.asset = task.request.asset;
                    }

                    cache.succeed = true;
                }
                else {
                    Debug.LogError($"[AssetDB] 加载资源: {task.url} 失败!");
                }
            }

            if (task.webOperation != null) {
                if (IsBinaryFile(task.url)) {
                    if (task.webOperation.webRequest.downloadHandler.data != null) {
                        cache.bytes = task.webOperation.webRequest.downloadHandler.data;
                        cache.succeed = true;
                    }
                    else {
                        Debug.LogError($"[AssetDB] 加载资源: {task.url} 失败!");
                    }
                }
                else {
                    if (!string.IsNullOrEmpty(task.webOperation.webRequest.downloadHandler.text)) {
                        cache.text = task.webOperation.webRequest.downloadHandler.text;
                        cache.succeed = true;
                    }
                    else {
                        Debug.LogError($"[AssetDB] 加载资源: {task.url} 失败!");
                    }
                }
            }

            assetCaches[assetCacheCount++] = cache;
        }
    }
}