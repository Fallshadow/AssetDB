using FallShadow.Common;
using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {
	public partial class AssetDB {
#if UNITY_EDITOR
		private struct RequestEditorAssetTask {
			public Handle<UAsset> handle;
			public FixedString512Bytes url;
		}

		private int requestEditorAssetTaskCount;
		private NativeArray<RequestEditorAssetTask> requestEditorAssetTasks;

		private struct RequestEditorSceneTask {
			public Handle<UAsset> handle;
			public Scene scene;
		}

		private int requestEditorSceneTaskCount;
		private NativeArray<RequestEditorSceneTask> requestEditorSceneTasks;

		private void InitEditor() {
            requestEditorAssetTasks = new NativeArray<RequestEditorAssetTask>(maxAssetCount, Allocator.Persistent);
            requestEditorAssetTaskCount = 0;
            requestEditorSceneTasks = new NativeArray<RequestEditorSceneTask>(maxSceneTaskCount, Allocator.Persistent);
            requestEditorSceneTaskCount = 0;
        }

		private void DisposeEditor() {
            if (requestEditorAssetTasks.IsCreated) {
                requestEditorAssetTasks.Dispose();
            }

            if (requestEditorSceneTasks.IsCreated) {
                requestEditorSceneTasks.Dispose();
            }
        }

        private void TickRequestEditorAssetTasks() {
			if (requestFileTaskCount > 0) {
				return;
			}

			for (var t = 0; t < requestEditorAssetTaskCount; t++) {
				var task = requestEditorAssetTasks[t];

				if (!handleManager.IsValid(task.handle)) {
					requestEditorAssetTaskConsumeAt(ref t);
					continue;
				}

				var relativePath = FixedStringUtil.Substring(task.url, protocolSep.Length);
				var assetPath = new FixedString512Bytes($"assets/{relativePath}");
				var lastIndex = assetPath.LastIndexOf('.');
				var ext = FixedStringUtil.Substring(assetPath, lastIndex, assetPath.Length - lastIndex);
				var assetPathStr = assetPath.ToString();
				var extStr = ext.ToString();

				var cache = new AssetCache {
					handle = task.handle,
					url = task.url
				};

				if (!ext2AssetType.TryGetValue(extStr, out var type)) {
					Debug.LogError($"[AssetDB] AssetDatabase 扩展名未注册 url: {task.url}, ext: {ext}");
					cache.succeed = false;
				}

				if (type == typeof(Scene)) {
					if (url2SceneInfo.TryGetValue(task.url, out var sceneInfo)) {
						bool isRepeatRequest = false;
                        foreach (var editorSceneTask in requestEditorSceneTasks) {
                            if (editorSceneTask.scene.path != null && editorSceneTask.scene.path == assetPath) {
                                Debug.LogWarning($"[AssetDB] 正在加载的场景任务中已经存在 {assetPath} 请检查是否重复请求场景！");
                                isRepeatRequest = true;
                                break;
                            }
                        }

						if (isRepeatRequest) {
                            requestEditorAssetTaskConsumeAt(ref t);
							continue;
                        }

                        // https://docs.unity.cn/cn/current/ScriptReference/SceneManagement.EditorSceneManager.LoadSceneInPlayMode.html
                        // 此函数与 SceneManager.LoadScene 的行为相同，这意味着加载不会立即发生，但保证在 <<下一帧中完成>>，此行为还意味着返回的场景的状态设置为 Loading
                        var scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(assetPath.ToString(), new LoadSceneParameters(sceneInfo.loadSceneMode));

						requestEditorSceneTasks[requestEditorSceneTaskCount++] = new RequestEditorSceneTask() {
							handle = task.handle,
							scene = scene
						};
					}
					else {
						Debug.LogError($"[AssetDB] 从 AssetDatabase 加载 {task.url} 失败，请通过 LoadScene 接口加载场景");
						cache.succeed = false;
					}
				}
				else if (type != typeof(TextAsset)) {
					// gc 88B
					var result = AssetDatabase.LoadAssetAtPath(assetPathStr, type);
					if (result == null) {
						Debug.LogError($"[AssetDB] 从 AssetDatabase 加载 {task.url} 资源失败");
						cache.succeed = false;
					}
					else {
						cache.asset = result;
						cache.succeed = true;
					}
				}
				else if (File.Exists(assetPathStr)) {
					if (IsBinaryFile(task.url)) {
						var bytes = File.ReadAllBytes(assetPathStr);
						cache.bytes = bytes;
						cache.succeed = true;
					}
					else {
						var text = File.ReadAllText(assetPathStr);
						cache.text = text;
						cache.succeed = true;
					}
				}
				else {
					Debug.LogError($"[AssetDB] AssetDatabase 不存在资源 {task.url}");
					cache.succeed = false;
				}

 				assetCaches[assetCacheCount++] = cache;

				requestEditorAssetTaskConsumeAt(ref t);
			}
		}

		private void TickRequestEditorSceneTasks() {
			if (requestFileTaskCount > 0) {
				return;
			}

			for (var t = 0; t < requestEditorSceneTaskCount; t++) {
				var task = requestEditorSceneTasks[t];

				if (!task.scene.isLoaded) {
					continue;
				}

				for (uint cacheIndex = 0; cacheIndex < assetCacheCount; cacheIndex++) {
					ref var cache = ref assetCaches[cacheIndex];

					if (!cache.handle.Equals(task.handle)) {
						continue;
					}

					assetCaches[cacheIndex].succeed = true;
					break;
				}

				requestEditorSceneTaskConsumeAt(ref t);
			}
		}

		private void requestEditorAssetTaskConsumeAt(ref int t) {
			requestEditorAssetTasks[t] = requestEditorAssetTasks[requestEditorAssetTaskCount - 1];
			requestEditorAssetTasks[requestEditorAssetTaskCount - 1] = default;
			requestEditorAssetTaskCount--;
			t--;
		}

		private void requestEditorSceneTaskConsumeAt(ref int t) {
			requestEditorSceneTasks[t] = requestEditorSceneTasks[requestEditorSceneTaskCount - 1];
			requestEditorSceneTasks[requestEditorSceneTaskCount - 1] = default;
			requestEditorSceneTaskCount--;
			t--;
		}
#endif
	}
}