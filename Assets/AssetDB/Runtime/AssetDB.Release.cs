using FallShadow.Common;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        public void Release(Handle<UAsset> handle) {
            ReleaseInternal(handle, false);
        }

        private void ReleaseByUrl(FixedString512Bytes url, bool forceClearRefCount) {
            url = FixedStringUtil.ToLower(url);

            if (!url2handle.TryGetValue(url, out var handle)) {
                return;
            }

            ReleaseInternal(handle, forceClearRefCount);
        }

        private void ReleaseInternal(Handle<UAsset> handle, bool forceClearRefCount) {
            if (!handleManager.IsValid(handle)) {
                return;
            }

            var url = GetUrlByHandle(handle);

#if UNITY_EDITOR
            if (loadMode == LoadMode.Editor) {
                refCounts[handle.index]--;

                if (url2SceneInfo.ContainsKey(url)) {
                    url2SceneInfo.Remove(url);
                }

                if (refCounts[handle.index] <= 0) {
                    handleManager.Free(handle);
                    url2handle.Remove(url);
                }

                Debug.LogWarning($"[AssetDB] Release From AssetDatabase: {url} refCount {refCounts[handle.index]}");
                return;
            }
#endif
            var taskType = GetTaskType(url);

            if (taskType.Equals(TaskType.None)) {
                handleManager.Free(handle);
                url2handle.Remove(url);

                if (url2SceneInfo.ContainsKey(url)) {
                    url2SceneInfo.Remove(url);
                }

                return;
            }

            if (taskType.Equals(TaskType.Bundle)) {
                var bundleKey = GetBundleKey(url);
                ReleaseBundleHandle(handle, bundleKey, forceClearRefCount);
            }
            else {
                ReleaseAssetHandle(handle, url, forceClearRefCount);
            }
        }


        private void ReleaseAssetHandle(Handle<UAsset> handle, FixedString512Bytes url, bool forceClearRefCount) {
            var refCount = refCounts[handle.index];
            refCount = forceClearRefCount ? 0 : --refCount;
            refCounts[handle.index] = refCount;

            if (refCount > 0) {
                return;
            }

            for (var i = 0; i < assetTaskCount; i++) {
                ref var task = ref assetTasks[i];

                if (handle.Equals(task.handle)) {
                    assetTasks[i] = assetTasks[assetTaskCount - 1];
                    assetTasks[assetTaskCount - 1] = default;
                    assetTaskCount--;
                    i--;
                }
            }

            for (uint i = 0; i < sceneTaskCount; i++) {
                ref var task = ref sceneTasks[i];

                if (handle.Equals(task.handle)) {
                    sceneTasks[i] = sceneTasks[sceneTaskCount - 1];
                    sceneTasks[sceneTaskCount - 1] = default;
                    sceneTaskCount--;
                    i--;
                }
            }

            for (uint i = 0; i < assetCacheCount; i++) {
                ref var cache = ref assetCaches[i];

                if (!cache.handle.Equals(handle)) {
                    continue;
                }

                if (cache.asset != null && !(cache.asset is GameObject)) {
                    Resources.UnloadAsset(cache.asset);
                }

                assetCaches[i] = assetCaches[assetCacheCount - 1];
                assetCaches[assetCacheCount - 1] = default;
                assetCacheCount--;
                i--;
            }

            handleManager.Free(handle);
            url2handle.Remove(url);

            if (url2SceneInfo.ContainsKey(url)) {
                url2SceneInfo.Remove(url);
            }

            if (handle2SceneName.TryGetValue(handle.index, out var name)) {
                var activeScene = SceneManager.GetActiveScene();
                var nameStr = name.ToString();

                if (activeScene.name != nameStr) {
                    var unloadScene = SceneManager.GetSceneByName(nameStr);

                    if (unloadScene.IsValid() && unloadScene.isLoaded) {
                        // 直接异步卸载，不处理句柄
                        SceneManager.UnloadSceneAsync(nameStr);
                    }
                }

                handle2SceneName.Remove(handle.index);
            }

            var taskType = GetTaskType(url);

            if (taskType.Equals(TaskType.Asset)) {
                return;
            }

            var bundleKey = GetBundleKey(url);
            var bundleUrl = $"{protocolSep}{bundleKey}";

            if (!url2handle.TryGetValue(bundleUrl, out var bundleHandle)) {
                // 存在 Load BundleAsset 后立即 Release，此时其所属的 Bundle 相关信息尚未生成
                return;
            }

            ReleaseBundleHandle(bundleHandle, bundleKey, forceClearRefCount);
        }

        private void ReleaseBundleHandle(Handle<UAsset> handle, FixedString512Bytes bundleKey, bool forceClearRefCount, bool unloadAsset = true) {
            var refCount = refCounts[handle.index];
            refCount = forceClearRefCount ? 0 : --refCount;
            refCounts[handle.index] = refCount;

            if (refCount > 0) {
                return;
            }

            // 如果是 LoadAllAssets，依次释放其持有的 allAssetHandles，并清除自身数据
            if (handle2AllAssetHandles.TryGetValue(handle, out var allAssetHandles)) {
                foreach (var assetHandle in allAssetHandles) {
                    ReleaseInternal(assetHandle, forceClearRefCount);
                }

                allAssetHandles.Dispose();
                handle2AllAssetHandles.Remove(handle);
                handleManager.Free(handle);
                url2handle.Remove($"{protocolSep}{bundleKey}{loadAllAssetsSep}");
                return;
            }

            for (var i = 0; i < bundleTaskCount; i++) {
                ref var task = ref bundleTasks[i];

                if (task.handle.Equals(handle)) {
                    task.markAsUnload = true;
                }
            }

            handleManager.Free(handle);
            url2handle.Remove($"{protocolSep}{bundleKey}");

            var bundle = index2Bundle[handle.index];

            if (bundle != null) {
#if USE_WECHAT_LOAD && WECHAT && !UNITY_EDITOR
                bundle.WXUnload(unloadAsset);
#else
                bundle.Unload(unloadAsset);
#endif
                index2Bundle.Remove(handle.index);
            }

            // 依赖处理
            if (bundleKey2Deps.TryGetValue(bundleKey, out var deps)) {
                foreach (var dep in deps) {
                    var depUrl = $"{protocolSep}{dep}";

                    // 强制清空时，存在相同依赖的数据，已经被清除的情况
                    if (forceClearRefCount && !url2handle.ContainsKey(depUrl)) {
                        continue;
                    }

                    var depHandle = url2handle[depUrl];
                    ReleaseBundleHandle(depHandle, dep, forceClearRefCount, unloadAsset);
                }
            }
        }
    }
}