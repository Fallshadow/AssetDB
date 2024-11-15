using FallShadow.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // 这是资源加载系统的第一阶段
    // 外部告知资源清单，这里读取并处理资源清单文件
    // 最终输出在函数 AddUrl2AssetInfo 中
    // 将信息整理成，url2AssetInfo bundleKey2Assets
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
        // 目前申请的资源清单数量
        private int requestFileTaskCount = 0;
        // 初始设置的最大的资源清单数量
        private const int maxFileTaskCount = 4;
        // 初始设置的最大的资源 Bundle 数量
        private const int maxFileBundleCount = 16;
        // 初始设置的最大的单 Bundle 内资源数量
        private const int maxFileBundleContainCount = 16;
        // 资源清单任务
        private RequestFileTask[] requestFileTasks;

        // file.txt 资源列表文件中的资源路径都是带 32 位 hash 的，需要转换为不带 hash 的。
        // 比如 scenes_8dda37713dd29cbf4dd8502556ba3db2.bundle
        // 转换后就会变成 scenes.bundle
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> hash2normal;

        // file.txt 资源列表文件提取出来的 bundle 资源信息
        // 比如 scenes_8dda37713dd29cbf4dd8502556ba3db2.bundle
        // 收集到的字串都放到这里，作用仅仅是 GlobFiles 中使用
        private NativeList<FixedString512Bytes> resourceBundleFilePaths;


        // 资源简要路径和资源详细属性的映射
        // example 1:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle" 非具体资源所以没有 assetPath
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        // 一个 bundle 对应多个资源 Asset
        // 这里的 bundle 是类似 "graphics/pipelines.bundle" 这种不带前缀也不带 hash 的，里面的资源也都是相对于 bundle 的。
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

        // AssetDB 第一时间需要加载的资源清单
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
                                Debug.LogWarning($"[AssetDB] 加载 {task.url} 文件失败!");
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

                    // 下载任务完成，如果任务是 file.txt 解析其中资源，生成
                    // specialFilePaths （总资源文件路径信息）
                    // hash2normal （带 hash 的文件路径 和 正常文件路径 字典）

                    switch (task.type) {
                        case FileType.Files:
                            unsafe {
                                fixed (byte* p = bytes) {
                                    var cursor = 0;
                                    var length = bytes.Length;
                                    var lastRelativePath = new FixedString512Bytes();
                                    var bundle2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(16, Allocator.Temp);

                                    // 逐行读取 资源清单 内容
                                    // bundle 放到 resourceBundleFilePaths 中
                                    // bundle 每个对应的资源放到临时容器 bundle2Assets 中
                                    for (var i = 0; i < length; i++) {
                                        var item = p[i];

                                        if (item == '\n') {
                                            var offset = i - cursor;

                                            if (offset == 0) {
                                                cursor = i + 1;
                                                continue;
                                            }

                                            var line = FixedStringUtil.GetFixedString512(p, cursor, offset);

                                            // 检查最后一个字符是否是回车，是的话就削减
                                            if (line[^1] == '\r') {
                                                line.Length -= 1;
                                            }

                                            // 检查第一个字符是否是 tab，是的话处理 file.txt 的 bundle To asset
                                            if (line[0] == '\t') {
                                                if (lastRelativePath.Length == 0) {
                                                    cursor = i + 1;
                                                    Debug.LogError($"[AssetDB] {line} 此资源不存在 bundle，请检查 files.txt");
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

                                    // 收集 BundleAssets 数据
                                    foreach (var kv in bundle2Assets) {
                                        var relativePath = kv.Key;
                                        var assets = kv.Value;

                                        if (!relativePath.EndsWith(bundleSep) || assets.Length == 0) {
                                            continue;
                                        }

                                        // 尽量获取去掉了 hash 的 bundle key
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
        /// 通过 file.txt 的 bundle2bytes 生成 bundleKey2Assets url2AssetInfo
        /// bundleKey2Assets 大的 bundle 目录 对应 很多资源路径
        /// url2AssetInfo 不仅仅包括大的 bundle 目录对应的 AssetInfo，还包含每一个文件具体 url 对应的 AssetInfo
        /// </summary>
        /// <param name="bundleKey"> file.txt 的一个 bundle 名称（不带 hash 的）比如 prefabs.bundle </param>
        /// <param name="bytes"> 对应的多个文件组成的 bytes，就是每个文件名（相对于 bundle 的路径名）都打碎成 bytes 依次排列到一起</param>
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
                            // ^1 从字符串最后一个字符开始计数 '\r' 回车
                            if (assetPath[^1] == '\r') {
                                assetPath.Length -= 1;
                            }

                            // 比如这个 prefabs_5bc8cda1412da08012d6191f0fc6856c.bundle key
                            // 处理后变成 asset://prefabs/ 加上具体资源路径
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