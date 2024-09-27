using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {

        // 协议 url 和资源信息的映射，用来加载资源
        internal NativeHashMap<FixedString512Bytes, Handle<UAsset>> url2handle;

        public Handle<UAsset> Load(string url) {
            return Load(new FixedString512Bytes(url));
        }

        public Handle<UAsset> Load(FixedString512Bytes url) {
            if (!url.StartsWith(protocolSep)) {
                throw new Exception($"[AssetDB][Load] invalid url: {url}, examples: asset://foo/bar.prefab");
            }

#if UNITY_EDITOR
            if (loadMode == LoadMode.Editor && url.EndsWith(bundleSep)) {
                Debug.LogError($"[AssetDB] AssetDatabase 模拟加载不支持 bundle 结尾的协议: {url}");
                return default;
            }
#endif
            url = FixedStringUtil.ToLower(url);

            if (url2handle.TryGetValue(url, out var handle)) {
                if (handleManager.IsValid(handle)) {
                    refCounts[handle.index]++;
                    return handle;
                }
            }

            return CreateRequestAssetTask(url);
        }

        private Handle<UAsset> CreateRequestAssetTask(FixedString512Bytes url) {
            Handle<UAsset> handle = handleManager.Malloc();
            url2handle[url] = handle;
            refCounts[handle.index] = 1;

            switch (loadMode) {
                case LoadMode.Runtime:
                    requestAssetTasks[requestAssetTaskCount++] = new RequestAssetTask() {
                        handle = handle,
                        url = url
                    };
                    break;
                case LoadMode.Editor:
#if UNITY_EDITOR
                    Debug.LogWarning($"[AssetDB] Begin Load From AssetDatabase: {url}");
                    requestEditorAssetTasks[requestEditorAssetTaskCount++] = new RequestEditorAssetTask() {
                        handle = handle,
                        url = url
                    };
#endif
                    break;
            }

            return handle;
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

        public Object GetAssetFromCache(Handle<UAsset> handle) {
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
    }
}