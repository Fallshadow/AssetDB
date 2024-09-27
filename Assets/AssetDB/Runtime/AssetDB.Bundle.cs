using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {

        private int depsTaskCount;
        private DepsTask[] depsTasks;

        private bool RequestBundleTask(FixedString512Bytes bundleKey) {
            for (var i = 0; i < bundleTaskCount; i++) {
                ref var task = ref bundleTasks[i];

                if (task.bundleKey == bundleKey) {
                    task.markAsUnload = false;
                    return true;
                }
            }

            var bundleUrl = $"{protocolSep}{bundleKey}";

            if (url2handle.TryGetValue(bundleUrl, out var handle)) {
                if (index2Bundle.ContainsKey(handle.index)) {
                    return true;
                }
            }

            var bundleFilePath = GetBundleFilePath(bundleKey);

            if (string.IsNullOrEmpty(bundleFilePath)) {
                Debug.LogError($"[AssetDB] 查找 bundle: {bundleKey} 失败!");
                return false;
            }

            depsTasks[depsTaskCount++] = new DepsTask() {
                bundleKey = bundleKey,
            };

            if (!handleManager.IsValid(handle)) {
                handle = handleManager.Malloc();
                url2handle[bundleUrl] = handle;
                refCounts[handle.index] = 0;
            }

            bundleTasks[bundleTaskCount++] = new BundleTask {
                handle = handle,
                bundleKey = bundleKey,
                isRemoteAsset = IsRemoteBundle(bundleKey)
            };

            return true;
        }

        // bundleKey: "foo/bar/foobar.bundle"
        // filePath: fileKey2FilePath[fileKey]
        private string GetBundleFilePath(FixedString512Bytes bundleKey) {
            if (fileKey2FilePath.TryGetValue(bundleKey, out var path)) {
                return path;
            }

            return null;
        }

        // bundleKey: "foo/bar/foobar.bundle"
        private bool IsRemoteBundle(FixedString512Bytes bundleKey) {
            if (fileKey2FilePath.TryGetValue(bundleKey, out var path)) {
                return path.StartsWith(httpsSep) || path.StartsWith(httpSep);
            }

            return false;
        }

        private void TickBundleTasks() {
            for (var bundleTaskIndex = 0; bundleTaskIndex < bundleTaskCount; bundleTaskIndex++) {
                ref var task = ref bundleTasks[bundleTaskIndex];

                if (!bundleKey2Deps.ContainsKey(task.bundleKey))
                    continue;

                var dependencies = bundleKey2Deps[task.bundleKey];

                if (!task.isRemoteAsset) {
                    if (task.createRequest == null) {
                        if (dependencies.Length > 0) {
                            foreach (var t in dependencies) {
                                RequestBundleTask(t);
                            }
                        }

                        task.createRequest = AssetBundle.LoadFromFileAsync(GetBundleFilePath(task.bundleKey));
                    }
                }
                else {
                    if (task.webRequestAsyncOperation == null) {
                        if (dependencies.Length > 0) {
                            foreach (var t in dependencies) {
                                RequestBundleTask(t);
                            }
                        }

#if USE_WECHAT_LOAD && WECHAT && !UNITY_EDITOR
                        var request = WXAssetBundle.GetAssetBundle(GetBundleFilePath(task.bundleKey));
#else
                        var request = UnityWebRequestAssetBundle.GetAssetBundle(GetBundleFilePath(task.bundleKey));
#endif
                        task.webRequestAsyncOperation = request.SendWebRequest();
                    }
                }

                var allDependencyDone = true;

                foreach (var dependency in dependencies) {
                    var bundleUrl = $"{protocolSep}{dependency}";

                    if (!url2handle.TryGetValue(bundleUrl, out var handle)) {
                        allDependencyDone = false;
                        break;
                    }

                    if (!index2Bundle.ContainsKey(handle.index)) {
                        allDependencyDone = false;
                        break;
                    }
                }

                // 依赖未加载完
                if (!allDependencyDone) {
                    continue;
                }

                AssetBundle assetBundle;

                if (!task.isRemoteAsset) {
                    if (task.createRequest is { isDone: false })
                        continue;

                    assetBundle = task.createRequest.assetBundle;
                }
                else {
                    if (task.webRequestAsyncOperation is { isDone: false })
                        continue;

#if USE_WECHAT_LOAD && WECHAT && !UNITY_EDITOR
                    assetBundle =
                        (task.webRequestAsyncOperation.webRequest.downloadHandler as DownloadHandlerWXAssetBundle)
                        .assetBundle;
#else
                    assetBundle =
                        (task.webRequestAsyncOperation.webRequest.downloadHandler as DownloadHandlerAssetBundle)
                        .assetBundle;
#endif
                }

                if (assetBundle == null) {
                    Debug.LogError($"[AssetDB] 加载 bundle: {task.bundleKey} 失败!");
                }
                else {
                    if (task.markAsUnload) {
                        assetBundle.Unload(true);
                    }
                    else {
                        var bundleUrl = $"{protocolSep}{task.bundleKey}";
                        var handle = url2handle[bundleUrl];
                        index2Bundle[handle.index] = assetBundle;

                        // 更新该 bundle 依赖的引用计数
                        foreach (var dep in dependencies) {
                            var depUrl = $"{protocolSep}{dep}";
                            var depHandle = url2handle[depUrl];
                            refCounts[depHandle.index]++;
                        }
                    }
                }

                bundleTaskConsumeAt(ref bundleTaskIndex);
            }
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

        private void TickSceneTasks() {
            for (int i = 0; i < sceneTaskCount; i++) {
                ref var task = ref sceneTasks[i];

                if (handle2SceneName.ContainsKey(task.handle.index)) {
                    sceneTaskConsumeAt(ref i);
                    continue;
                }

#if USE_ENTITIES
                if (!task.catalogIsLoaded) {
                    task.catalogIsLoaded = true;
                    var catalog = GetContentCatalogPath(task.bundleUrl);

                    if (catalog != null) {
                        RuntimeContentManager.LoadLocalCatalogData(catalog, RuntimeContentManager.DefaultContentFileNameFunc, EntityScenePathsPublic.ArchivePathFunc);
                    }
                }
#endif

                if (task.asyncOperation == null) {
                    task.asyncOperation = SceneManager.LoadSceneAsync(task.sceneInfo.sceneName.ToString(), task.sceneInfo.loadSceneMode);
                }

                if (task.asyncOperation.isDone) {
                    handle2SceneName[task.handle.index] = task.sceneInfo.sceneName;
                    sceneTaskConsumeAt(ref i);
                }
            }
        }
    }
}