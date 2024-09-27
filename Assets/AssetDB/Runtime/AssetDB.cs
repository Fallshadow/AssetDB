using FallShadow.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FallShadow.Asset.Runtime {

    public struct UAsset { }


    // ������Ҫ�����Դ·����Դ����
    // Unity �����£���Դ������ Sandbox �£�file.txt Ϊ��Դ�嵥��
    // 
    public partial class AssetDB {

        public enum LoadMode {
            Editor,
            Runtime
        }
        private LoadMode loadMode;

        private bool needInvokeGlobFiles;

        // ��Դ����·��  mount��"d:/app/sandbox"
        // �ļ����ؼ�   filekey: "arts/fonts/font1.bundle"
        // ��Դ����·�� filepath: "d:/app/sandbox/arts/fonts/font1.bundle"
        // ���������ʹ�������·��
        private NativeList<FixedString512Bytes> mounts;
        // Զ����Դ·��
        // ���� {remoteConfig.remoteUrl}/StreamingAssets
        // �� https://funny-iaa.funnyrpg.com/InfiniteTrain/0.0.3/StreamingAssets
        private FixedString512Bytes specialDirectory;

        // �ļ���Զ�·�����ļ���·��֮���ӳ��
        private Dictionary<FixedString512Bytes, string> fileKey2FilePath;

        // Ŀǰ��Ҫ������ļ���������
        private int requestFileTaskCount = 0;
        // ����Զ�̻򱾵��൱�� StreamingAssets �µ���Դ
        private RequestFileTask[] requestFileTasks;
        // file.txt ��Դ�б��ļ���ȡ��������Դ��Ϣ
        private NativeList<FixedString512Bytes> specialFilePaths;
        // file.txt ��Դ�б��ļ��е���Դ·�����Ǵ� 32 λ hash �ģ���Ҫת��Ϊ���� hash �ġ�
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> hash2normal;
        // example 1:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        // һ�� bundle ��Ӧ�����Դ Asset
        private NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Assets;

        // �ļ���չ����Ӧ��Դ���� extension to AssetType
        // ���� .prefab ��Ӧ GameObject
        private static Dictionary<FixedString32Bytes, Type> ext2AssetType;
        // Ϊ�������ļ���������һ���б���ⴢ����Щ��չ��
        private static NativeList<FixedString32Bytes> binaryAssetExts;

        // TODO

        private int bundleRequestTaskCount;
        private BundleRequestTask[] bundleRequestTasks;

        private BundleTask[] bundleTasks;
        private int bundleTaskCount;
        private Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Deps;
        private int requestAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAssetTasks;
        private int requestAllAssetTaskCount;
        private NativeArray<RequestAssetTask> requestAllAssetTasks;
        private AssetTask[] assetTasks;
        private int assetTaskCount;
        private AssetCache[] assetCaches;
        private int assetCacheCount;
        private SceneTask[] sceneTasks;
        private int sceneTaskCount;
        private NativeHashMap<FixedString512Bytes, SceneInfo> url2SceneInfo;
        private Dictionary<int, FixedString32Bytes> handle2SceneName;
        private ushort releaseBundleCounter;
        private NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes> subSceneFileName2Path;
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> url2ContentCatalogPath;



        // examples: "https://s3.sofunny.io/forbar/v1.0.1/StreamingAssets"

        internal HandleManager<UAsset> handleManager;

        

        private NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>> handle2AllAssetHandles;

        // index: handle.index, value: refCount
        internal NativeArray<int> refCounts;
        // example:
        // key: new Handle<UAsset>(1, 1).index
        // Value: AssetBundle
        internal Dictionary<int, AssetBundle> index2Bundle;

        private const int maxMountCount = 16;
        private const int maxBundleRequesTaskCount = 256;

        // TODO
        private const int prefAssetTypeCount = 16;
        private const int maxBundleTaskCount = 256;
        private const int maxAllAssetTaskCount = 32;
        private const int maxAssetTaskCount = 256;
        private const int maxSceneTaskCount = 32;
        private const int maxSubSceneFileCount = 128;
        private const int maxSubSceneCatalogCount = 16;
        private const int maxFileTaskCount = 4;
        private const int maxAssetCount = 4096;



        public void Initialize(LoadMode mode = LoadMode.Runtime) {
            loadMode = mode;
            if (loadMode == LoadMode.Editor) {
                Debug.LogWarning("[AssetDB][Initialize] �༭��ģʽ������ AssetDatabase ģ�����");
            }

            needInvokeGlobFiles = false;

            mounts = new NativeList<FixedString512Bytes>(maxMountCount, Allocator.Persistent);
            fileKey2FilePath = new Dictionary<FixedString512Bytes, string>();

            ext2AssetType = new Dictionary<FixedString32Bytes, Type>(prefAssetTypeCount);
            binaryAssetExts = new NativeList<FixedString32Bytes>(prefAssetTypeCount, Allocator.Persistent);

            // bundle
            bundleRequestTasks = new BundleRequestTask[maxBundleRequesTaskCount];
            bundleTasks = new BundleTask[maxBundleTaskCount];
            bundleKey2Deps = new Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>>();

            // load
            requestAssetTasks = new NativeArray<RequestAssetTask>(maxAssetTaskCount, Allocator.Persistent);
            assetTasks = new AssetTask[maxAssetTaskCount];

            // load all
            requestAllAssetTasks = new NativeArray<RequestAssetTask>(maxAllAssetTaskCount, Allocator.Persistent);

            // load scene
            sceneTasks = new SceneTask[maxSceneTaskCount];
            url2SceneInfo = new NativeHashMap<FixedString512Bytes, SceneInfo>(maxSceneTaskCount, Allocator.Persistent);
            handle2SceneName = new Dictionary<int, FixedString32Bytes>();

            // sub scene
            subSceneFileName2Path = new NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes>(maxSubSceneFileCount, Allocator.Persistent);
            url2ContentCatalogPath = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxSubSceneCatalogCount, Allocator.Persistent);

            // files
            requestFileTasks = new RequestFileTask[maxFileTaskCount];
            specialFilePaths = new NativeList<FixedString512Bytes>(maxFileTaskCount, Allocator.Persistent);
            specialDirectory = Application.streamingAssetsPath.Replace("\\", sep.ToString());

            // asset
            handleManager = new HandleManager<UAsset>(maxAssetCount);
            url2handle = new NativeHashMap<FixedString512Bytes, Handle<UAsset>>(maxAssetCount, Allocator.Persistent);
            url2AssetInfo = new NativeHashMap<FixedString512Bytes, AssetInfo>(maxAssetCount, Allocator.Persistent);
            bundleKey2Assets = new NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>>(maxAssetCount, Allocator.Persistent);
            handle2AllAssetHandles = new NativeParallelHashMap<Handle<UAsset>, NativeList<Handle<UAsset>>>(maxAssetCount, Allocator.Persistent);
            hash2normal = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxAssetCount, Allocator.Persistent);
            refCounts = new NativeArray<int>(maxAssetCount, Allocator.Persistent);
            index2Bundle = new Dictionary<int, AssetBundle>();
            assetCaches = new AssetCache[maxAssetCount];

            bundleRequestTaskCount = 0;
            bundleTaskCount = 0;
            requestAllAssetTaskCount = 0;
            requestAssetTaskCount = 0;
            assetTaskCount = 0;
            assetCacheCount = 0;
            sceneTaskCount = 0;
            requestFileTaskCount = 0;
            releaseBundleCounter = 0;

#if UNITY_EDITOR
            requestEditorAssetTasks = new NativeArray<RequestEditorAssetTask>(maxAssetCount, Allocator.Persistent);
            requestEditorAssetTaskCount = 0;
            requestEditorSceneTasks = new NativeArray<RequestEditorSceneTask>(maxSceneTaskCount, Allocator.Persistent);
            requestEditorSceneTaskCount = 0;
#endif
        }

        public void RegisterAsset(FixedString32Bytes extension, Type type, bool isBinary) {
            ext2AssetType[extension] = type;

            if (isBinary) {
                if (!binaryAssetExts.Contains(extension)) {
                    binaryAssetExts.Add(extension);
                }
            }
        }

        public void SetSpecialDirectory(FixedString512Bytes url) {
            if (url.Length == 0) {
                throw new Exception($"invalid url: {url}");
            }

            specialDirectory = url;
        }

        public void SetFilesUrl(FixedString512Bytes url) {
            CreateRequestFileTask(url, FileType.Files);
        }

        // AssetDB ��һʱ����Ҫ���ص���Դ
        private void CreateRequestFileTask(FixedString512Bytes relativeFilePath, FileType type) {
            requestFileTasks[requestFileTaskCount++] = new RequestFileTask {
                type = type,
                url = relativeFilePath
            };
        }

        public void Mount(FixedString512Bytes dirPath) {
            mounts.Add(FixedStringUtil.Replace(new FixedString512Bytes(dirPath), '\\', '/'));
        }

        public void UnMount(FixedString512Bytes dirPath) {
            if (!mounts.IsCreated) return;

            FixedString512Bytes unmount = FixedStringUtil.Replace(new FixedString512Bytes(dirPath), '\\', '/');
            int index = mounts.IndexOf(unmount);

            if (index == -1) return;

            mounts.RemoveAt(index);

            foreach (var kv in fileKey2FilePath) {
                FixedString512Bytes fileKey = kv.Key;

                if (fileKey.Length <= bundleSep.Length) {
                    continue;
                }

                if (!kv.Value.StartsWith(unmount.ToString())) {
                    continue;
                }

                if (!FixedStringUtil.Substring(fileKey, fileKey.Length - bundleSep.Length).Equals(bundleSep)) {
                    ReleaseByUrl($"asset://{fileKey}", true);
                    continue;
                }

                // ���Խ��� bundle �µ�������Դ Release
                foreach (var kv1 in url2AssetInfo) {
                    if (fileKey != kv1.Value.bundleKey) {
                        continue;
                    }

                    ReleaseByUrl(kv1.Key, true);
                }

                if (bundleKey2Deps.TryGetValue(fileKey, out var deps)) {
                    bundleKey2Deps.Remove(fileKey);
                    deps.Dispose();
                }
            }
        }

        public void Dispose() {
            if (bundleKey2Deps != null) {
                foreach (var dependencies in bundleKey2Deps.Values) {
                    if (dependencies.IsCreated) {
                        dependencies.Dispose();
                    }
                }

                bundleKey2Deps.Clear();
                bundleKey2Deps = null;
            }

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

            if (mounts.IsCreated) {
                mounts.Dispose();
            }

            ext2AssetType.Clear();
            ext2AssetType = null;

            if (binaryAssetExts.IsCreated) {
                binaryAssetExts.Dispose();
            }

            if (fileKey2FilePath != null) {
                fileKey2FilePath.Clear();
                fileKey2FilePath = null;
            }

            depsTasks = null;

            if (requestAllAssetTasks.IsCreated) {
                requestAllAssetTasks.Dispose();
            }

            if (requestAssetTasks.IsCreated) {
                requestAssetTasks.Dispose();
            }

            bundleTasks = null;
            sceneTasks = null;
            requestFileTasks = null;

            if (subSceneFileName2Path.IsCreated) {
                subSceneFileName2Path.Dispose();
            }

            if (url2ContentCatalogPath.IsCreated) {
                url2ContentCatalogPath.Dispose();
            }

            if (index2Bundle != null) {
                foreach (var bundle in index2Bundle.Values) {
                    if (bundle != null) {
                        bundle.Unload(true);
                    }
                }
            }

            index2Bundle = null;
            handle2SceneName = null;

            if (handle2AllAssetHandles.IsCreated) {
                foreach (var kv in handle2AllAssetHandles) {
                    if (kv.Value.IsCreated) {
                        kv.Value.Dispose();
                    }
                }

                handle2AllAssetHandles.Dispose();
            }

            if (hash2normal.IsCreated) {
                hash2normal.Dispose();
            }

            if (url2SceneInfo.IsCreated) {
                url2SceneInfo.Dispose();
            }

            if (specialFilePaths.IsCreated) {
                specialFilePaths.Dispose();
            }

            if (handleManager.IsCreated()) {
                handleManager.Dispose();
            }

            if (url2handle.IsCreated) {
                url2handle.Dispose();
            }

            if (refCounts.IsCreated) {
                refCounts.Dispose();
            }

#if UNITY_EDITOR
            if (requestEditorAssetTasks.IsCreated) {
                requestEditorAssetTasks.Dispose();
            }

            if (requestEditorSceneTasks.IsCreated) {
                requestEditorSceneTasks.Dispose();
            }
#endif
        }

        public void GlobFiles() {
            if (requestFileTaskCount > 0) {
                needInvokeGlobFiles = true;
                return;
            }

            subSceneFileName2Path.Clear();
            url2ContentCatalogPath.Clear();

            foreach (var mount in mounts) {
                var dirPath = mount.ToString();
                string[] files;

                // �õ������ԴĿ¼�µ������ļ�
                if (dirPath.StartsWith(specialDirectory.ToString())) {
                    var list = new List<string>();

                    if (specialFilePaths.IsCreated) {
                        foreach (var path in specialFilePaths) {
                            var fullPath = $"{specialDirectory}/{path}";
                            list.Add(fullPath);
                        }
                    }

                    files = list.ToArray();
                }
                else {
                    if (!Directory.Exists(dirPath)) {
                        continue;
                    }

                    files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                }


                var length = mount.Length;
                var isRemoteDirectory = dirPath.StartsWith(httpsSep) || dirPath.StartsWith(httpSep);
                var isStreamingAssets = dirPath.StartsWith(Application.streamingAssetsPath.Replace("\\", sep.ToString()));

                foreach (var file in files) {
                    var fsFile = FixedStringUtil.Replace(new FixedString512Bytes(file), '\\', '/');
                    // ��ȡ���ļ���Զ�·��
                    var relativePath = FixedStringUtil.Substring(fsFile, length + 1);


                    // subScene
                    if (relativePath.IndexOf(entitySceneDir) != -1 || relativePath.IndexOf(contentArchiveDir) != -1) {
                        if (relativePath.IndexOf(contentFileName) != -1) {
                            var relativeDirectory = FixedStringUtil.Substring(relativePath, 0, relativePath.Length - contentFileName.Length);

                            FixedString512Bytes url = protocolSep;
                            url.Append(FixedStringUtil.ToLower(relativeDirectory));
                            url.Append(bundleSep);

                            url2ContentCatalogPath[url] = fsFile;
                        }
                        else {
                            var fileName = Path.GetFileName(file);
                            subSceneFileName2Path[fileName] = fsFile;
                        }
                    }
                    else {
                        if (hash2normal.ContainsKey(relativePath)) {
                            relativePath = hash2normal[relativePath];
                        }

                        relativePath = FixedStringUtil.ToLower(relativePath);
                        var index = relativePath.IndexOf(bundleSep);
                        var isBundleFile = index != -1 && index + bundleSep.Length == relativePath.Length;

                        if (isBundleFile) {
                            fileKey2FilePath[relativePath] = fsFile.ToString();
                        }
                        else {
                            if (isRemoteDirectory) {
                                fileKey2FilePath[relativePath] = fsFile.ToString();
                            }
                            else {
                                switch (Application.platform) {
                                    case RuntimePlatform.Android:
                                        if (isStreamingAssets) {
                                            fileKey2FilePath[relativePath] = fsFile.ToString();
                                        }
                                        else {
                                            fileKey2FilePath[relativePath] = $"file://{fsFile}";
                                        }
                                        break;
                                    case RuntimePlatform.LinuxPlayer:
                                    case RuntimePlatform.OSXPlayer:
                                    case RuntimePlatform.OSXEditor:
                                    case RuntimePlatform.IPhonePlayer:
                                        fileKey2FilePath[relativePath] = $"file://{fsFile}";
                                        break;
                                    default:
                                        fileKey2FilePath[relativePath] = fsFile.ToString();
                                        break;
                                }
                            }
                        }
                    }
                }
            }
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

        private void TickRequestFileTasks() {
            for (var t = 0; t < requestFileTaskCount; t++) {
                ref var task = ref requestFileTasks[t];

                if (task.operation == null) {
                    string fileUrl;
                    var isRemoteAssets = task.url.StartsWith(new FixedString512Bytes(httpsSep)) || task.url.StartsWith(new FixedString512Bytes(httpSep));
                    if (!isRemoteAssets) {
#if UNITY_EDITOR
                        fileUrl = $"file://{task.url}";
#else
                        if (Application.platform == RuntimePlatform.Android) {
                            fileUrl = task.url.ToString();
                        } else {
                            fileUrl = $"file://{task.url}";
                        }
#endif
                    }
                    else {
                        fileUrl = task.url.ToString();
                    }

                    var webReq = UnityWebRequest.Get(fileUrl);
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
                                                    Debug.LogError($"[AssetDB] {lastRelativePath} �����ڣ����� files.txt");
                                                    continue;
                                                }

                                                if (!bundle2Assets.TryGetValue(lastRelativePath, out var assets)) {
                                                    assets = new NativeList<FixedString512Bytes>(16, Allocator.Temp);
                                                    bundle2Assets[lastRelativePath] = assets;
                                                }

                                                assets.Add(FixedStringUtil.Substring(line, 1));
                                                cursor = i + 1;
                                                continue;
                                            }

                                            var relativePath = line;
                                            lastRelativePath = relativePath;
                                            specialFilePaths.Add(relativePath);

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

            if (requestFileTaskCount == 0 && needInvokeGlobFiles) {
                needInvokeGlobFiles = false;
                GlobFiles();
            }
        }



        /// <summary>
        /// ͨ�� file.txt �� bundle2bytes ���� bundleKey2Assets url2AssetInfo
        /// bundleKey2Assets ��� bundle Ŀ¼ ��Ӧ �ܶ���Դ·��
        /// url2AssetInfo ������������� bundle Ŀ¼��Ӧ�� AssetInfo��������ÿһ���ļ����� url ��Ӧ�� AssetInfo
        /// </summary>
        /// <param name="bundleKey"> file.txt ��һ�� bundle ��Ӧ����ļ� </param>
        /// <param name="bytes"> ��Ӧ�Ķ���ļ���ɵ� bytes </param>
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
                    assets = new NativeList<FixedString512Bytes>(16, Allocator.Persistent);
                    bundleKey2Assets[bundleKey] = assets;
                }

                for (var i = 0; i < length; i++) {
                    var item = ptr[i];

                    if (item == '\n') {
                        FixedString512Bytes assetPath = FixedStringUtil.GetFixedString512(ptr, cursor, i - cursor);

                        if (assetPath.Length > 0) {
                            if (assetPath[^1] == '\r') {
                                assetPath.Length -= 1;
                            }

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

        private void TickRequestAssetTasks() {
            if (requestFileTaskCount > 0) {
                return;
            }

            for (int t = 0; t < requestAssetTaskCount; t++) {
                RequestAssetTask requestAssetTask = requestAssetTasks[t];

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

                        if (!RequestBundleTask(bundleKey)) {
                            Release(requestAssetTask.handle);
                            // ������ assetTasks
                            requestAssetTaskConsumeAt(ref t);
                            continue;
                        }

                        break;
                }
            }
        }

        



        private void TickDepsTasks() {
            for (var t = 0; t < depsTaskCount; t++) {
                ref var task = ref depsTasks[t];

                if (bundleKey2Deps.ContainsKey(task.bundleKey)) {
                    requestDepsTaskConsumeAt(ref t);
                    continue;
                }

                if (task.webOperation == null) {
                    var filePath = GetDepsFilePath(task.bundleKey);

                    if (string.IsNullOrEmpty(filePath)) {
                        bundleKey2Deps[task.bundleKey] = new NativeList<FixedString512Bytes>(0, Allocator.Persistent);
                        requestDepsTaskConsumeAt(ref t);
                        continue;
                    }

                    var request = UnityWebRequest.Get(filePath);
                    task.webOperation = request.SendWebRequest();
                }

                if (task.webOperation.isDone && task.webOperation.webRequest.isDone) {
                    if (string.IsNullOrEmpty(task.webOperation.webRequest.error)) {
                        var bytes = task.webOperation.webRequest.downloadHandler.data;
                        AddDeps(task.bundleKey, bytes);
                    }

                    task.webOperation = null;
                    requestDepsTaskConsumeAt(ref t);
                }
            }
        }

        // bundleKey: "foo/bar/foobar.bundle"
        // fileKey: "foo/bar/foobar.deps"
        // filePath: fileKey2FilePath[fileKey]
        private string GetDepsFilePath(FixedString512Bytes bundleKey) {
            var index = bundleKey.IndexOf(bundleSep);

            if (index == -1) {
                return null;
            }

            var fileKey = FixedStringUtil.Substring(bundleKey, 0, index);
            fileKey.Append(depsSep);

            if (fileKey2FilePath.TryGetValue(fileKey, out var path)) {
                return path;
            }

            return null;
        }

        private void AddDeps(FixedString512Bytes bundleKey, byte[] bytes) {
            if (bytes == null || bytes.Length == 0) {
                return;
            }

            unsafe {
                var ptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0).ToPointer();
                var cursor = 0;
                var length = bytes.Length;
                NativeList<FixedString512Bytes> deps;

                if (bundleKey2Deps.ContainsKey(bundleKey)) {
                    deps = bundleKey2Deps[bundleKey];
                }
                else {
                    deps = new NativeList<FixedString512Bytes>(16, Allocator.Persistent);
                    bundleKey2Deps[bundleKey] = deps;
                }

                for (var i = 0; i < length; i++) {
                    var item = ptr[i];

                    if (item == '\n') {
                        var dep = FixedStringUtil.GetFixedString512(ptr, cursor, i - cursor);

                        if (dep[dep.Length - 1] == '\r') {
                            dep.Length -= 1;
                        }

                        deps.Add(dep);
                        cursor = i + 1;
                    }
                }
            }
        }

        // TODO
        public enum TaskType {
            Bundle,
            BundleAsset,
            Asset,
            None
        }

        private enum FileType {
            Files
        }

        private struct RequestAssetTask {
            public Handle<UAsset> handle;
            public FixedString512Bytes url;
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

        private struct RequestFileTask {
            public FileType type;
            public FixedString512Bytes url;
            public UnityWebRequestAsyncOperation operation;
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

