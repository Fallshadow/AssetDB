using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    // �ⲿ������Դ�ӿڣ�ֱ�Ӽ�����Դ bundle �µ�������Դ
    // ���� handle2AllAssetHandles ���ڴ�� handle �µ�ÿһ�� handle��ͨ�� request single asset ������Դ
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
                Debug.LogError($"[AssetDB] AssetDatabase ģ����ز�֧�� LoadAllAssets ����");
                return default;
            }
#endif

            url = FixedStringUtil.ToLower(url);
            // TODO: �����Ӽ��껹����ô��.bundle/* Ӧ��һ���Ҳ����ɡ�
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

                // ��¼ allHandle �������е� allAssetHandles ��ӳ��
                if (!handle2AllAssetHandles.TryGetValue(requestAllAssetTask.handle, out var allAssetHandles)) {
                    allAssetHandles = new NativeList<Handle<UAsset>>(16, Allocator.Persistent);
                    handle2AllAssetHandles[requestAllAssetTask.handle] = allAssetHandles;
                }

                foreach (var assetUrl in assets) {
                    //  assetUrl ��Ӧ�� handle �������򴴽��������񣬴��������ü��� ++
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