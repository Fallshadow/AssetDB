using FallShadow.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {

    public partial class AssetDB {
        private const int maxSceneTaskCount = 32;

        private NativeHashMap<FixedString512Bytes, SceneInfo> url2SceneInfo;

        private int sceneTaskCount;
        private SceneTask[] sceneTasks;

        private Dictionary<int, FixedString32Bytes> handle2SceneName;

        private void InitSceneTask() {
            sceneTaskCount = 0;
            sceneTasks = new SceneTask[maxSceneTaskCount];
            handle2SceneName = new Dictionary<int, FixedString32Bytes>();
            url2SceneInfo = new NativeHashMap<FixedString512Bytes, SceneInfo>(maxSceneTaskCount, Allocator.Persistent);
        }

        private void DisposeSceneTask() {
            sceneTasks = null;
            handle2SceneName = null;
            if (url2SceneInfo.IsCreated) {
                url2SceneInfo.Dispose();
            }
        }

        // example:
        // url: asset://foo/bar/scene/locomotion.unity
        public Handle<UAsset> LoadScene(FixedString512Bytes url, LoadSceneMode loadSceneMode = LoadSceneMode.Single) {
            if (!url.StartsWith(protocolSep)) {
                throw new Exception($"[AssetDB] invalid url: {url}, examples: asset://foo/bar.unity");
            }

            if (!url.EndsWith(sceneSep)) {
                throw new Exception($"[AssetDB] invalid scene url: {url}");
            }

            Debug.Log($"[AssetDB] 请求加载场景: {url}");

            url = FixedStringUtil.ToLower(url);
            url2SceneInfo[url] = new SceneInfo {
                loadSceneMode = loadSceneMode
            };

            if (url2handle.TryGetValue(url, out var handle)) {
                if (handleManager.IsValid(handle)) {
                    if (loadMode == LoadMode.Editor) {
#if UNITY_EDITOR
                        for (var i = 0; i < assetCacheCount; i++) {
                            ref var cache = ref assetCaches[i];

                            if (!cache.handle.Equals(handle)) {
                                continue;
                            }

                            assetCachesConsumeAt(ref i);
                            break;
                        }

                        refCounts[handle.index] = 0;
                        handleManager.Free(handle);
                        url2handle.Remove(url);
                        return CreateRequestAssetTask(url);
#endif
                    }

                    refCounts[handle.index]++;

                    // 如果是已成功加载过的场景，直接加载
                    if (handle2SceneName.TryGetValue(handle.index, out var sceneName)) {
                        handle2SceneName.Remove(handle.index);
                        sceneTasks[sceneTaskCount++] = new SceneTask {
                            handle = handle,
                            sceneInfo = new SceneInfo {
                                loadSceneMode = loadSceneMode,
                                sceneName = sceneName
                            },
                            catalogIsLoaded = true,
                            bundleUrl = url
                        };
                    }

                    return handle;
                }
            }

            return CreateRequestAssetTask(url);
        }

        private void TickSceneTasks() {
            for (int i = 0; i < sceneTaskCount; i++) {
                ref var task = ref sceneTasks[i];

                if (handle2SceneName.ContainsKey(task.handle.index)) {
                    sceneTaskConsumeAt(ref i);
                    continue;
                }

#if USE_ENTITIES
                if (!task.catalogIsLoaded) {
                    task.catalogIsLoaded = true;
                    var catalog = GetContentCatalogPath(task.bundleUrl);

                    if (catalog != null) {
                        RuntimeContentManager.LoadLocalCatalogData(catalog, RuntimeContentManager.DefaultContentFileNameFunc, EntityScenePathsPublic.ArchivePathFunc);
                    }
                }
#endif

                if (task.asyncOperation == null) {
                    task.asyncOperation = SceneManager.LoadSceneAsync(task.sceneInfo.sceneName.ToString(), task.sceneInfo.loadSceneMode);
                }

                if (task.asyncOperation.isDone) {
                    handle2SceneName[task.handle.index] = task.sceneInfo.sceneName;
                    Debug.Log($"[AssetDB] 加载场景完成: {task.sceneInfo.sceneName}");
                    sceneTaskConsumeAt(ref i);
                }
            }
        }
    }
}