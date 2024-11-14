using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    // 外部加载资源接口，直接加载资源 bundle 下的所有资源
    // 产出 handle2AllAssetHandles 对于大的 handle 下的每一项 handle，通过 request single asset 申请资源
    public partial class AssetDB {
        private const int maxAllAssetTaskCount = 32;

        private int requestAllAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAllAssetTasks;
        private NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>> handle2AllAssetHandles;

        private void InitRequestAllAsset() {
            requestAllAssetTaskCount = 0;
            requestAllAssetTasks = new NativeArray<RequestAssetTask>(maxAllAssetTaskCount, Allocator.Persistent);
            handle2AllAssetHandles = new NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>>(maxAssetCount, Allocator.Persistent);
        }

        private void DisposeRequestAllAsset() {
            if (requestAllAssetTasks.IsCreated) {
                requestAllAssetTasks.Dispose();
            }


            if (handle2AllAssetHandles.IsCreated) {
                foreach (var kv in handle2AllAssetHandles) {
                    if (kv.Value.IsCreated) {
                        kv.Value.Dispose();
                    }
                }

                handle2AllAssetHandles.Dispose();
            }
        }

        // example:
        // url: asset://foo/bar.bundle
        public Handle<UAsset> LoadAllAssets(FixedString512Bytes url) {
            if (!url.StartsWith(protocolSep)) {
                throw new Exception($"[AssetDB] invalid url: {url}, examples: asset://foo/bar.bundle");
            }

            if (!url.EndsWith(bundleSep)) {
                throw new Exception($"[AssetDB] invalid bundle url: {url}");
            }

#if UNITY_EDITOR
            if (loadMode == LoadMode.Editor) {
                Debug.LogError($"[AssetDB] AssetDatabase 模拟加载不支持 LoadAllAssets 方法");
                return default;
            }
#endif

            url = FixedStringUtil.ToLower(url);
            // TODO: 这样子加完还能用么？.bundle/* 应该一定找不到吧。
            url.Append(loadAllAssetsSep);

            if (url2handle.TryGetValue(url, out var handle)) {
                if (handleManager.IsValid(handle)) {
                    refCounts[handle.index]++;
                    return handle;
                }
            }

            return CreateRequestAllAssetTask(url);
        }

        private Handle<UAsset> CreateRequestAllAssetTask(FixedString512Bytes url) {
            Handle<UAsset> handle = handleManager.Malloc();
            url2handle[url] = handle;
            refCounts[handle.index] = 1;

            requestAllAssetTasks[requestAllAssetTaskCount++] = new RequestAssetTask() {
                handle = handle,
                url = url
            };

            return handle;
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
    }
}