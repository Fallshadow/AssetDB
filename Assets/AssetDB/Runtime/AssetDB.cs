using FallShadow.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {
    // ������Ҫ�����Դ·����Դ����
    // Unity �����£���Դ������ Sandbox �£�file.txt Ϊ��Դ�嵥��
    // ʹ�÷�����files �ļ��嵥�����ã�Ȼ�����ñ�ǣ������Ƿ�ֱ�Ӷ�ȡ������Դ
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
                Debug.LogWarning("[AssetDB][Initialize] �༭��ģʽ������ AssetDatabase ģ�����");
            }
            else if (loadMode == LoadMode.Runtime) {
                Debug.LogWarning("[AssetDB][Initialize] ����ģʽ��ֱ�Ӷ�ȡ��Դ");
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