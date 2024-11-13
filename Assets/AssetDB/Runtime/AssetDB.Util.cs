using FallShadow.Common;
using System;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        // url: "asset://foo/bar/foobar[.bin|.dll|.sdf]"
        private bool IsBinaryFile(FixedString512Bytes url) {
            foreach (var ext in binaryAssetExts) {
                var index = url.IndexOf(ext);

                if (index + ext.Length == url.Length) {
                    return true;
                }
            }

            return false;
        }

        // url1: "asset://foo/bar/foobar/car/data/element.toml"
        // bundleKey1: "foo/bar/foobar.bundle"
        //
        // url2: "asset://foo/bar/foobar/car/data/element.toml/*"
        // bundleKey2: "foo/bar/foobar.bundle"
        private FixedString512Bytes GetBundleKey(FixedString512Bytes url) {
            if (url2AssetInfo.ContainsKey(url)) {
                return url2AssetInfo[url].bundleKey;
            }

            Debug.LogError($"[AssetDB] 查找资源: {url} 所属的 bundle 失败, 请检查资源是否存在!");
            return default;
        }

        // url 1: "asset://foo/bar/foobar.bundle", url endsWith ".bundle"
        // taskType: Bundle
        //
        // url 2: "asset://foo/bar/foobar/foo/bar.toml"
        // fileKey: "foo/bar/foobar/foo/bar.toml"
        // taskType: if fileKey2FilePath contains fileKey then taskType is asset else taskType is bundleAsset
        //
        // url 3: "asset://foo/bar/foobar.toml"
        // taskType: asset
        //
        // url 4: "asset://foo/bar/foobar.bundle/*"
        // taskType: Bundle
        private TaskType GetTaskType(FixedString512Bytes url) {
            if (url2AssetInfo.ContainsKey(url)) {
                return url2AssetInfo[url].type;
            }

            var fileKey = FixedStringUtil.Substring(url, protocolSep.Length);

            if (fileKey2FilePath.ContainsKey(fileKey)) {
                return TaskType.Asset;
            }

            Debug.LogWarning($"[AssetDB] 查找资源: {url} 失败, 请检查资源是否存在!");
            return TaskType.None;
        }

        public FixedString512Bytes GetUrlByHandle(Handle<UAsset> handle) {
            foreach (var kv in url2handle) {
                if (kv.Value.Equals(handle)) {
                    return kv.Key;
                }
            }

            return default;
        }

        private Status GetStatus(Handle<UAsset> handle) {
            if (!handleManager.IsValid(handle)) {
                return Status.Invalid;
            }

            for (var i = 0; i < assetCacheCount; i++) {
                ref var cache = ref assetCaches[i];

                if (cache.handle.Equals(handle)) {
                    return cache.succeed ? Status.Succeeded : Status.Failed;
                }
            }

            for (var i = 0; i < assetTaskCount; i++) {
                ref var task = ref assetTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

            for (var i = 0; i < bundleTaskCount; i++) {
                ref var task = ref bundleTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

#if UNITY_EDITOR
            for (var i = 0; i < requestEditorAssetTaskCount; i++) {
                var task = requestEditorAssetTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

            for (var i = 0; i < requestEditorSceneTaskCount; i++) {
                var task = requestEditorSceneTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }
#endif

            for (var i = 0; i < requestAllAssetTaskCount; i++) {
                var task = requestAllAssetTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

            for (var i = 0; i < requestAssetTaskCount; i++) {
                var task = requestAssetTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

            for (uint i = 0; i < sceneTaskCount; i++) {
                ref var task = ref sceneTasks[i];

                if (task.handle.Equals(handle)) {
                    return Status.Loading;
                }
            }

            if (handle2SceneName.ContainsKey(handle.index)) {
                return Status.Succeeded;
            }

            if (index2Bundle.ContainsKey(handle.index)) {
                return Status.Succeeded;
            }

            if (handle2AllAssetHandles.TryGetValue(handle, out var handles)) {
                foreach (var assetHandle in handles) {
                    var status = GetStatus(assetHandle);

                    if (status != Status.Succeeded) {
                        return status;
                    }
                }

                return Status.Succeeded;
            }

            return Status.Failed;
        }

        // url: "asset://foo/bar/foobar/car/data/element.toml"
        // assetKey: "car/data/element.toml"
        // type: textAsset
        //
        // returns (assetKey, type)
        private (FixedString512Bytes, Type) GetBundleAssetPathAndType(FixedString512Bytes url) {
            var extIndex = url.LastIndexOf(extSep);

            if (extIndex == -1) {
                Debug.LogError($"[AssetDB] url: {url} 缺少后缀!");
                return (url, null);
            }

            unsafe {
                var length = url.Length - extIndex;
                FixedString32Bytes ext = default;
                var srcP = url.GetUnsafePtr();
                var dstP = ext.GetUnsafePtr();
                UTF8ArrayUnsafeUtility.Copy(dstP, out var len, FixedString32Bytes.UTF8MaxLengthInBytes, srcP + extIndex, length);
                ext.Length = len;

                if (!ext2AssetType.TryGetValue(ext, out var type)) {
                    Debug.LogError($"[AssetDB] 未知的后缀类型: {ext}!");
                    return (url, null);
                }

                if (url2AssetInfo.ContainsKey(url)) {
                    return (url2AssetInfo[url].assetPath, type);
                }

                return (default, type);
            }
        }

        // url 1: "asset://foo/bar/foobar/car/data/element.toml"
        // fileKey: "foo/bar/foobar/car/data/element.toml"
        // filePath: fileKey2FilePath[fileKey]
        //
        // url 2: "asset://gameplay/levels/farm.level"
        // fileKey: "gameplay/levels/farm.level"
        // filePath: fileKey2FilePath[fileKey]
        private string GetAssetFilePath(FixedString512Bytes url) {
            var fileKey = FixedStringUtil.Substring(url, protocolSep.Length);

            if (fileKey2FilePath.TryGetValue(fileKey, out var path)) {
                return path;
            }

            return null;
        }

        private void requestFileTaskConsumeAt(ref int t) {
            requestFileTasks[t] = requestFileTasks[requestFileTaskCount - 1];
            requestFileTasks[requestFileTaskCount - 1] = default;
            requestFileTaskCount--;
            t--;
        }

        private void requestAllAssetTaskConsumeAt(ref int t) {
            requestAllAssetTasks[t] = requestAllAssetTasks[requestAllAssetTaskCount - 1];
            requestAllAssetTasks[requestAllAssetTaskCount - 1] = default;
            requestAllAssetTaskCount--;
            t--;
        }

        private void requestAssetTaskConsumeAt(ref int t) {
            requestAssetTasks[t] = requestAssetTasks[requestAssetTaskCount - 1];
            requestAssetTasks[requestAssetTaskCount - 1] = default;
            requestAssetTaskCount--;
            t--;
        }

        private void requestDepsTaskConsumeAt(ref int t) {
            depsTasks[t] = depsTasks[depsTaskCount - 1];
            depsTasks[depsTaskCount - 1] = default;
            depsTaskCount--;
            t--;
        }

        private void bundleTaskConsumeAt(ref int t) {
            bundleTasks[t] = bundleTasks[bundleTaskCount - 1];
            bundleTasks[bundleTaskCount - 1] = default;
            bundleTaskCount--;
            t--;
        }

        private void assetTaskConsumeAt(ref int t) {
            assetTasks[t] = assetTasks[assetTaskCount - 1];
            assetTasks[assetTaskCount - 1] = default;
            assetTaskCount--;
            t--;
        }
        
        private void sceneTaskConsumeAt(ref int t) {
            sceneTasks[t] = sceneTasks[sceneTaskCount - 1];
            sceneTasks[sceneTaskCount - 1] = default;
            sceneTaskCount--;
            t--;
        }
    }
}