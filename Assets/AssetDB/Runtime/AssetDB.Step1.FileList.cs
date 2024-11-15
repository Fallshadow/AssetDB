using FallShadow.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // ������Դ����ϵͳ�ĵ�һ�׶�
    // �ⲿ��֪��Դ�嵥�������ȡ��������Դ�嵥�ļ�
    // ��������ں��� AddUrl2AssetInfo ��
    // ����Ϣ����ɣ�url2AssetInfo bundleKey2Assets
    public partial class AssetDB {
        private struct RequestFileTask {
            public FileType type;
            public FixedString512Bytes url;
            public UnityWebRequestAsyncOperation operation;

            public string GetRealUrl() {
                string fileUrl;
                var isRemoteAssets = url.StartsWith(new FixedString512Bytes(httpsSep)) || url.StartsWith(new FixedString512Bytes(httpSep));
                if (!isRemoteAssets) {
#if UNITY_EDITOR
                    fileUrl = $"file://{url}";
#else
                        if (Application.platform == RuntimePlatform.Android) {
                            fileUrl = url.ToString();
                        } else {
                            fileUrl = $"file://{url}";
                        }
#endif
                }
                else {
                    fileUrl = url.ToString();
                }

                return fileUrl;
            }
        }

        private bool needLoadGlobFiles;
        // Ŀǰ�������Դ�嵥����
        private int requestFileTaskCount = 0;
        // ��ʼ���õ�������Դ�嵥����
        private const int maxFileTaskCount = 4;
        // ��ʼ���õ�������Դ Bundle ����
        private const int maxFileBundleCount = 16;
        // ��ʼ���õ����ĵ� Bundle ����Դ����
        private const int maxFileBundleContainCount = 16;
        // ��Դ�嵥����
        private RequestFileTask[] requestFileTasks;

        // file.txt ��Դ�б��ļ��е���Դ·�����Ǵ� 32 λ hash �ģ���Ҫת��Ϊ���� hash �ġ�
        // ���� scenes_8dda37713dd29cbf4dd8502556ba3db2.bundle
        // ת����ͻ��� scenes.bundle
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> hash2normal;

        // file.txt ��Դ�б��ļ���ȡ������ bundle ��Դ��Ϣ
        // ���� scenes_8dda37713dd29cbf4dd8502556ba3db2.bundle
        // �ռ������ִ����ŵ�������ý����� GlobFiles ��ʹ��
        private NativeList<FixedString512Bytes> resourceBundleFilePaths;


        // ��Դ��Ҫ·������Դ��ϸ���Ե�ӳ��
        // example 1:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle" �Ǿ�����Դ����û�� assetPath
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        // һ�� bundle ��Ӧ�����Դ Asset
        // ����� bundle ������ "graphics/pipelines.bundle" ���ֲ���ǰ׺Ҳ���� hash �ģ��������ԴҲ��������� bundle �ġ�
        private NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Assets;

        private void InitFileTask() {
            requestFileTaskCount = 0;
            requestFileTasks = new RequestFileTask[maxFileTaskCount];
            resourceBundleFilePaths = new NativeList<FixedString512Bytes>(maxFileBundleCount, Allocator.Persistent);
            hash2normal = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxAssetCount, Allocator.Persistent);

            url2AssetInfo = new NativeHashMap<FixedString512Bytes, AssetInfo>(maxAssetCount, Allocator.Persistent);
            bundleKey2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(maxAssetCount, Allocator.Persistent);
        }

        private void DisposeFileTask() {
            requestFileTasks = null;
            if (resourceBundleFilePaths.IsCreated) resourceBundleFilePaths.Dispose();
            if (hash2normal.IsCreated) hash2normal.Dispose();

            if (url2AssetInfo.IsCreated) {
                url2AssetInfo.Dispose();
            }

            if (bundleKey2Assets.IsCreated) {
                foreach (var kv in bundleKey2Assets) {
                    if (kv.Value.IsCreated) {
                        kv.Value.Dispose();
                    }
                }

                bundleKey2Assets.Dispose();
            }
        }

        // AssetDB ��һʱ����Ҫ���ص���Դ�嵥
        public void AddFilesTask(FixedString512Bytes url) {
            requestFileTasks[requestFileTaskCount++] = new RequestFileTask {
                type = FileType.Files,
                url = url
            };
        }

        private void TickRequestFileTasks() {
            for (var t = 0; t < requestFileTaskCount; t++) {
                ref var task = ref requestFileTasks[t];

                if (task.operation == null) {
                    var webReq = UnityWebRequest.Get(task.GetRealUrl());
                    task.operation = webReq.SendWebRequest();
                }

                if (task.operation.isDone && task.operation.webRequest.isDone) {
                    if (!string.IsNullOrEmpty(task.operation.webRequest.error)) {
                        if (Application.isMobilePlatform || !specialDirectory.IsEmpty) {
                            if (loadMode == LoadMode.Runtime) {
                                Debug.LogWarning($"[AssetDB] ���� {task.url} �ļ�ʧ��!");
                            }
                        }

                        requestFileTaskConsumeAt(ref t);
                        continue;
                    }

                    var bytes = task.operation.webRequest.downloadHandler.data;

                    if (bytes == null || bytes.Length == 0) {
                        requestFileTaskConsumeAt(ref t);
                        continue;
                    }

                    // ����������ɣ���������� file.txt ����������Դ������
                    // specialFilePaths ������Դ�ļ�·����Ϣ��
                    // hash2normal ���� hash ���ļ�·�� �� �����ļ�·�� �ֵ䣩

                    switch (task.type) {
                        case FileType.Files:
                            unsafe {
                                fixed (byte* p = bytes) {
                                    var cursor = 0;
                                    var length = bytes.Length;
                                    var lastRelativePath = new FixedString512Bytes();
                                    var bundle2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(16, Allocator.Temp);

                                    // ���ж�ȡ ��Դ�嵥 ����
                                    // bundle �ŵ� resourceBundleFilePaths ��
                                    // bundle ÿ����Ӧ����Դ�ŵ���ʱ���� bundle2Assets ��
                                    for (var i = 0; i < length; i++) {
                                        var item = p[i];

                                        if (item == '\n') {
                                            var offset = i - cursor;

                                            if (offset == 0) {
                                                cursor = i + 1;
                                                continue;
                                            }

                                            var line = FixedStringUtil.GetFixedString512(p, cursor, offset);

                                            // ������һ���ַ��Ƿ��ǻس����ǵĻ�������
                                            if (line[^1] == '\r') {
                                                line.Length -= 1;
                                            }

                                            // ����һ���ַ��Ƿ��� tab���ǵĻ����� file.txt �� bundle To asset
                                            if (line[0] == '\t') {
                                                if (lastRelativePath.Length == 0) {
                                                    cursor = i + 1;
                                                    Debug.LogError($"[AssetDB] {line} ����Դ������ bundle������ files.txt");
                                                    continue;
                                                }

                                                if (!bundle2Assets.TryGetValue(lastRelativePath, out var assets)) {
                                                    assets = new NativeList<FixedString512Bytes>(maxFileBundleContainCount, Allocator.Temp);
                                                    bundle2Assets[lastRelativePath] = assets;
                                                }

                                                assets.Add(FixedStringUtil.Substring(line, 1));
                                                cursor = i + 1;
                                                continue;
                                            }

                                            var relativePath = line;
                                            lastRelativePath = relativePath;
                                            resourceBundleFilePaths.Add(relativePath);

                                            if (Common.FileUtil.FilePathExclude32Hash(relativePath, out var fileNoHashName)) {
                                                hash2normal[relativePath] = fileNoHashName;
                                            }

                                            cursor = i + 1;
                                        }
                                    }

                                    // �ռ� BundleAssets ����
                                    foreach (var kv in bundle2Assets) {
                                        var relativePath = kv.Key;
                                        var assets = kv.Value;

                                        if (!relativePath.EndsWith(bundleSep) || assets.Length == 0) {
                                            continue;
                                        }

                                        // ������ȡȥ���� hash �� bundle key
                                        if (!hash2normal.TryGetValue(relativePath, out var bundleKey)) {
                                            bundleKey = relativePath;
                                        }

                                        var byteList = new List<byte>();

                                        foreach (var asset in assets) {
                                            for (var l = 0; l < asset.Length; l++) {
                                                byteList.Add(asset[l]);
                                            }

                                            byteList.Add((byte)'\n');
                                        }

                                        AddUrl2AssetInfo(bundleKey, byteList.ToArray());
                                    }
                                }
                            }
                            break;
                    }
                    requestFileTaskConsumeAt(ref t);
                }
            }

            if (needLoadGlobFiles && requestFileTaskCount == 0) {
                needLoadGlobFiles = false;
                GlobFiles();
            }
        }

        /// <summary>
        /// ͨ�� file.txt �� bundle2bytes ���� bundleKey2Assets url2AssetInfo
        /// bundleKey2Assets ��� bundle Ŀ¼ ��Ӧ �ܶ���Դ·��
        /// url2AssetInfo ������������� bundle Ŀ¼��Ӧ�� AssetInfo��������ÿһ���ļ����� url ��Ӧ�� AssetInfo
        /// </summary>
        /// <param name="bundleKey"> file.txt ��һ�� bundle ���ƣ����� hash �ģ����� prefabs.bundle </param>
        /// <param name="bytes"> ��Ӧ�Ķ���ļ���ɵ� bytes������ÿ���ļ���������� bundle ��·������������� bytes �������е�һ��</param>
        private void AddUrl2AssetInfo(FixedString512Bytes bundleKey, byte[] bytes) {
            if (bytes == null || bytes.Length == 0) {
                return;
            }

            unsafe {
                var ptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0).ToPointer();
                var cursor = 0;
                var length = bytes.Length;

                url2AssetInfo[$"{protocolSep}{bundleKey}"] = new AssetInfo {
                    type = TaskType.Bundle,
                    bundleKey = bundleKey
                };

                url2AssetInfo[$"{protocolSep}{bundleKey}{loadAllAssetsSep}"] = new AssetInfo {
                    type = TaskType.Bundle,
                    bundleKey = bundleKey
                };

                if (!bundleKey2Assets.TryGetValue(bundleKey, out var assets)) {
                    assets = new NativeList<FixedString512Bytes>(maxFileBundleContainCount, Allocator.Persistent);
                    bundleKey2Assets[bundleKey] = assets;
                }

                for (var i = 0; i < length; i++) {
                    var item = ptr[i];

                    if (item == '\n') {
                        FixedString512Bytes assetPath = FixedStringUtil.GetFixedString512(ptr, cursor, i - cursor);

                        if (assetPath.Length > 0) {
                            // ^1 ���ַ������һ���ַ���ʼ���� '\r' �س�
                            if (assetPath[^1] == '\r') {
                                assetPath.Length -= 1;
                            }

                            // ������� prefabs_5bc8cda1412da08012d6191f0fc6856c.bundle key
                            // ������� asset://prefabs/ ���Ͼ�����Դ·��
                            FixedString512Bytes assetUrl = $"{protocolSep}{FixedStringUtil.Substring(bundleKey, 0, bundleKey.Length - bundleSep.Length)}/{assetPath}";
                            assets.Add(assetUrl);
                            url2AssetInfo[assetUrl] = new AssetInfo {
                                type = TaskType.BundleAsset,
                                bundleKey = bundleKey,
                                assetPath = assetPath
                            };
                        }

                        cursor = i + 1;
                    }
                }
            }
        }
    }
}