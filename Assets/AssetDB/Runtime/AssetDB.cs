using FallShadow.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {
    // 首先需要理解资源路径资源名称
    // Unity 环境下，资源放置在 Sandbox 下，file.txt 为资源清单。
    // 使用方法：files 文件清单先设置，然后设置标记，决定是否直接读取所有资源
    public partial class AssetDB {

        public enum LoadMode {
            Editor,
            Runtime
        }
        private LoadMode loadMode;

        public void RegisterAsset(FixedString32Bytes extension, Type type, bool isBinary) {
            ext2AssetType[extension] = type;

            if (isBinary) {
                if (!binaryAssetExts.Contains(extension)) {
                    binaryAssetExts.Add(extension);
                }
            }
        }

        public void Initialize(bool needLoadGlobFiles = true, LoadMode mode = LoadMode.Runtime) {
            loadMode = mode;
            if (loadMode == LoadMode.Editor) {
                Debug.LogWarning("[AssetDB][Initialize] 编辑器模式，开启 AssetDatabase 模拟加载");
            }
            else if (loadMode == LoadMode.Runtime) {
                Debug.LogWarning("[AssetDB][Initialize] 运行模式，直接读取资源");
            }

            this.needLoadGlobFiles = needLoadGlobFiles;

            InitUtil();
            InitFileTask();
            InitMount();
            InitRequestSingleAsset();
            InitRequestAllAsset();
            InitRequestBundleTask();
            InitDeps();
            InitSceneTask();
            InitHandle();

            InitEcsContainer();
#if UNITY_EDITOR
            InitEditor();
#endif
        }

        public void Dispose() {
            DisposeUtil();
            DisposeFileTask();
            DisposeMount();
            DisposeRequestSingleAsset();
            DisposeRequestAllAsset();
            DisposeRequestBundleTask();
            DisposeDeps();
            DisposeSceneTask();
            DisposeHandle();

            DisposeEcsContainer();
#if UNITY_EDITOR
            DisposeEditor();
#endif
        }


        public void Tick() {
            TickRequestFileTasks();
#if UNITY_EDITOR
            TickRequestEditorAssetTasks();
            TickRequestEditorSceneTasks();
#endif
            TickRequestAllAssetTasks();
            TickRequestAssetTasks();

            TickDepsTasks();
            TickBundleTasks();
            TickAssetTasks();
            TickSceneTasks();
            // AutoReleaseBundle();
        }

        public enum TaskType {
            Bundle,
            BundleAsset,
            Asset,
            None
        }

        private enum FileType {
            Files
        }

        private struct SceneInfo {
            public FixedString32Bytes sceneName;
            public LoadSceneMode loadSceneMode;
        }

        private struct SceneTask {
            public Handle<UAsset> handle;
            public SceneInfo sceneInfo;
            public FixedString512Bytes bundleUrl;
            public bool catalogIsLoaded;
            public AsyncOperation asyncOperation;
        }

        internal struct AssetInfo {
            public TaskType type;
            public FixedString512Bytes bundleKey;
            public FixedString512Bytes assetPath;
        }

        public enum Status {
            Invalid,
            Loading,
            Succeeded,
            Failed
        }
    }
}