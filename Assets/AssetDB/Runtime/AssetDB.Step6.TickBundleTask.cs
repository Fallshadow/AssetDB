using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // Ϊ�� Bundle ���� request ����Ϊ�������� BundleTask
    // ����������ɺ󣬲鿴�� ab �Ƿ������ɣ������֮��
    // ���� index2Bundle��������������
    public partial class AssetDB {
        private void TickBundleTasks() {
            for (var bundleTaskIndex = 0; bundleTaskIndex < bundleTaskCount; bundleTaskIndex++) {
                ref var task = ref bundleTasks[bundleTaskIndex];

                if (!bundleKey2Deps.ContainsKey(task.bundleKey))
                    continue;

                var dependencies = bundleKey2Deps[task.bundleKey];

                // ��Զ����Դ���������� request����Ϊ������Ҳ���� BundleTask
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
                // Զ����Դ��unity �������� request��wx api ��ȡ��Դ����Ϊ������Ҳ���� BundleTask
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

                    // TODO����������д�Ļ����� allDependencyDone ������Զ�� false?
                    if (!index2Bundle.ContainsKey(handle.index)) {
                        allDependencyDone = false;
                        break;
                    }
                }

                // ����δ������
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
                    Debug.LogError($"[AssetDB] ���� bundle: {task.bundleKey} ʧ��!");
                }
                else {
                    if (task.markAsUnload) {
                        assetBundle.Unload(true);
                    }
                    else {
                        var bundleUrl = $"{protocolSep}{task.bundleKey}";
                        var handle = url2handle[bundleUrl];
                        index2Bundle[handle.index] = assetBundle;

                        // ���¸� bundle ���������ü���
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