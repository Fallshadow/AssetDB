using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;
using static FallShadow.Asset.Runtime.AssetDB;

namespace FallShadow.Asset.Runtime {
    // �ⲿ������Դ�ӿڣ�ֱ�Ӽ��ص�����Դ
    // �ⲿ������Դ·���������Դȷʵ�Ϸ����ڣ�������������Դ������
    // ���� url2handle refCounts �ȴ�ʵ����Դ��λ
    // ���� bundle ���ͣ�Tick request ������ BundleTask
    // ������Դ���ͣ������������Դ���� assetTask

    // ������ȷŵ��� handle �Ǳ��ٽ��е� BundleTask������ Bundle �Ǳ�Ӧ��ֱ�ӿ��Դ� handle �õ� �� index2Bundle ����û��
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
                Debug.LogError($"[AssetDB] AssetDatabase ģ����ز�֧�� bundle ��β��Э��: {url}");
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

                // �Ƿ�����ɾ������
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

                        // ���� bundle key ��û���ҵ���Դ���ͷ� handle
                        if (!RequestBundleTask(bundleKey)) {
                            Release(requestAssetTask.handle);
                            requestAssetTaskConsumeAt(ref t);
                            continue;
                        }
                        break;
                }

                // Load Bundle ʱ���贴�� AssetTask��ֻ����Դ�����ߵ�����
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