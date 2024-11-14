using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // 为本 Bundle 创建 request 任务，为依赖创建 BundleTask
    // 依赖加载完成后，查看本 ab 是否加载完成，都完成之后
    // 产出 index2Bundle，增加依赖计数
    public partial class AssetDB {
        private void TickBundleTasks() {
            for (var bundleTaskIndex = 0; bundleTaskIndex < bundleTaskCount; bundleTaskIndex++) {
                ref var task = ref bundleTasks[bundleTaskIndex];

                if (!bundleKey2Deps.ContainsKey(task.bundleKey))
                    continue;

                var dependencies = bundleKey2Deps[task.bundleKey];

                // 非远程资源，创建本地 request，并为依赖项也创建 BundleTask
                if (!task.isRemoteAsset) {

                    if (task.createRequest == null) {
                        if (dependencies.Length > 0) {
                            foreach (var t in dependencies) {
                                RequestBundleTask(t);
                            }
                        }

                        task.createRequest = AssetBundle.LoadFromFileAsync(GetMountBundleFilePath(task.bundleKey));
                    }
                }
                // 远程资源，unity 创建本地 request，wx api 获取资源，并为依赖项也创建 BundleTask
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
                        var request = UnityWebRequestAssetBundle.GetAssetBundle(GetMountBundleFilePath(task.bundleKey));
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

                    // TODO：这条这样写的话，那 allDependencyDone 岂不是永远是 false?
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
    }
}