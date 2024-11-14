using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;
using static FallShadow.Asset.Runtime.AssetDB;

namespace FallShadow.Asset.Runtime {
    // 外部加载资源接口，直接加载单个资源
    // 外部传入资源路径，如果资源确实合法存在，建立起请求资源的任务
    // 产出 url2handle refCounts 等待实际资源落位
    // 对于 bundle 类型，Tick request 建立起 BundleTask
    // 对于资源类型，创建具体的资源任务 assetTask

    // 这边是先放到了 handle 那边再进行的 BundleTask，所以 Bundle 那边应该直接可以从 handle 得到 但 index2Bundle 可能没有
    public partial class AssetDB {
        private int requestAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAssetTasks;

        private void InitRequestSingleAsset() {
            requestAssetTaskCount = 0;
            requestAssetTasks = new NativeArray<RequestAssetTask>(maxAssetTaskCount, Allocator.Persistent);
        }

        private void DisposeRequestSingleAsset() {
            if (requestAssetTasks.IsCreated) {
                requestAssetTasks.Dispose();
            }
        }

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

        private void TickRequestAssetTasks() {
            if (requestFileTaskCount > 0) {
                return;
            }

            for (int t = 0; t < requestAssetTaskCount; t++) {
                RequestAssetTask requestAssetTask = requestAssetTasks[t];

                // 非法请求，删除请求
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

                        // 拿着 bundle key 都没有找到资源，释放 handle
                        if (!RequestBundleTask(bundleKey)) {
                            Release(requestAssetTask.handle);
                            requestAssetTaskConsumeAt(ref t);
                            continue;
                        }
                        break;
                }

                // Load Bundle 时无需创建 AssetTask，只有资源才能走到这里
                if (!taskType.Equals(TaskType.Bundle)) {
                    assetTasks[assetTaskCount++] = new AssetTask() {
                        handle = requestAssetTask.handle,
                        url = requestAssetTask.url
                    };
                }

                requestAssetTaskConsumeAt(ref t);
            }
        }
    }
}