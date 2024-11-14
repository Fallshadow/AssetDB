using FallShadow.Common;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // 创建 BundleTask 并产出 depsTasks
    public partial class AssetDB {
        private const int MaxBundleTaskCount = 1024;

        private int bundleTaskCount;
        private BundleTask[] bundleTasks;

        public struct BundleTask {
            public Handle<UAsset> handle;
            public FixedString512Bytes bundleKey;
            public bool markAsUnload;
            public AssetBundleCreateRequest createRequest;
            public bool isRemoteAsset;
            public UnityWebRequestAsyncOperation webRequestAsyncOperation;
        }

        private void InitRequestBundleTask() {
            bundleTaskCount = 0;
            bundleTasks = new BundleTask[MaxBundleTaskCount];
        }

        private void DisposeRequestBundleTask() {
            bundleTasks = null;
        }

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

            var bundleFilePath = GetMountBundleFilePath(bundleKey);

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
                isRemoteAsset = IsMountRemoteBundle(bundleKey)
            };

            return true;
        }
    }
}