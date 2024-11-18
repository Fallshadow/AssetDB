using Framework.Shares;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace FallShadow.Asset.Editor {
    public static class Builder {
        public const string BundleFileExt = ".bundle";
        public const string BundleAssetsFileExt = ".assets";
        public const string BundleDepsFileExt = ".deps";

        public static readonly string[] PkgTargets = {
            "bundle", "subscene", "file", "dylib", "zip"
        };

        private static string _defaultOutput;
        public static string DefaultOutput {
            get {
                if (_defaultOutput != null) {
                    return _defaultOutput;
                }

                ParsePkgMake();
                return _defaultOutput;
            }
        }

        private static string _pkgflowPath;
        public static string PkgflowPath {
            get {
                if (_pkgflowPath != null) {
                    return _pkgflowPath;
                }

                ParsePkgMake();
                return _pkgflowPath;
            }
        }

        private static string[] _ignorePatterns;
        public static string[] IgnorePatterns {
            get {
                if (_ignorePatterns != null) {
                    return _ignorePatterns;
                }

                ParsePkgMake();
                return _ignorePatterns;
            }
        }

        private static void ParsePkgMake() {
            string pkgMakeFolder = $"{Application.dataPath}/..";
            string pkgMakePath = $"{pkgMakeFolder}/pkgmake";

            if (File.Exists(pkgMakePath)) {
                string pkgStr = File.ReadAllText(pkgMakePath);
                using TomlDocument pkgDoc = TomlCS.Parse(pkgStr);
                string[] ignoreArray = pkgDoc.GetStringArray("ignores-pattern") ?? Array.Empty<string>();
                _ignorePatterns = new string[ignoreArray.Length];

                for (int i = 0; i < ignoreArray.Length; i++) {
                    _ignorePatterns[i] = ignoreArray[i];
                }

                _defaultOutput = pkgDoc.GetString("default-output");

                if (string.IsNullOrEmpty(_defaultOutput)) {
                    _defaultOutput = "sandbox";
                }

                _defaultOutput = $"{pkgMakeFolder}/{_defaultOutput}";

                _pkgflowPath = pkgDoc.GetString("pkgflowDir");

                if (string.IsNullOrEmpty(_pkgflowPath)) {
                    _pkgflowPath = ".pkgflow";
                }

                _pkgflowPath = $"{pkgMakeFolder}/{_pkgflowPath}";
            }
            else {
                _ignorePatterns = Array.Empty<string>();
                _defaultOutput = $"{pkgMakeFolder}/sandbox";
                _pkgflowPath = $"{pkgMakeFolder}/.pkgflow";
            }
        }

        public static void Exec(PkgHelper pkgHelper, string pkgPath, bool clearManifest, string outputPath = "") {
            if (string.IsNullOrEmpty(outputPath)) {
                outputPath = DefaultOutput;
            }

            if (!Directory.Exists(outputPath)) {
                Directory.CreateDirectory(outputPath);
            }

            var targetTypes = pkgHelper.GetTargetTypes(pkgPath);

            if (targetTypes.Length == 0) {
                Debug.LogWarning($"{pkgPath} 中不存在 {Command.GetActivePlatform()} 平台可构建的内容");
                return;
            }

            foreach (var targetType in targetTypes) {
                // 获取对应 targetType 的所有 targetPaths
                var targetPaths = pkgHelper.GetTargetPaths(pkgPath, targetType);

                if (targetPaths.Length == 0) {
                    continue;
                }

                foreach (var targetPath in targetPaths) {
                    Debug.Log($"开始构建 {targetType}: {targetPath}");
                    Exec(pkgHelper, targetPath, targetType, clearManifest, outputPath);
                    Debug.Log($"成功构建 {targetType}: {targetPath}");
                }
            }
        }

        public static void Exec(PkgHelper pkgHelper, string targetPath, string targetType,
            bool clearManifest, string outputPath) {
            // targetPath : foo/bar/Prefabs
            if (targetType.Equals(PkgTargets[0])) {
                // depsPaths: { "foo/bar/Prefabs_Dep/Materials" }
                string[] depsPaths = pkgHelper.GetBundleDeps(targetPath);
                string[] bundleTargetPaths = new string[depsPaths.Length + 1];
                depsPaths.CopyTo(bundleTargetPaths, 0);
                bundleTargetPaths[^1] = targetPath;
                BuildBundles(pkgHelper, bundleTargetPaths, targetType, outputPath);
            }
            else if (targetType.Equals(PkgTargets[1])) {
                BuildBundles(pkgHelper, new[] { targetPath }, targetType, outputPath);
                BuildSubScenes(pkgHelper, targetPath, outputPath);
            }
            else if (targetType.Equals(PkgTargets[2])) {
                CopyConfigs(pkgHelper, targetPath, outputPath);
            }
            else if (targetType.Equals(PkgTargets[3])) {
                BuildDlls(pkgHelper, targetPath, outputPath);
            }
            else if (targetType.Equals(PkgTargets[4])) {
                // TODO
            }

            if (clearManifest) {
                ClearManifests(outputPath);
            }
        }

        private static void BuildBundles(PkgHelper pkgHelper, string[] targetPaths, string targetType,
    string outputPath) {
            var builds = new AssetBundleBuild[targetPaths.Length];
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.AppendHashToAssetBundleName;
            for (var i = 0; i < targetPaths.Length; i++) {
                var targetPath = targetPaths[i];
                var assets = pkgHelper.GetTargetAssets(targetPath, targetType);

                if (assets.Length == 0) {
                    Debug.LogWarning($"该 {targetPath} 未匹配到任何资源, 请检查 patterns 配置!");
                    continue;
                }

                var build = GenerateBundleBuild(targetPath, assets, targetPath);
                builds[i] = build;

                string[] depsPaths = pkgHelper.GetBundleDeps(targetPath);

                var target = new BuildCacheTarget {
                    path = targetPath,
                    type = targetType,
                    depends = new List<string>(depsPaths),
                    files = new List<string>()
                };

                BuildCacheData.AddBuildTarget(target);
            }

            // 清除旧的资源
            foreach (var build in builds) {
                // bundlePath: foo/bar/prefabs.bundle
                var bundlePath = Path.GetFullPath($"{outputPath}/{build.assetBundleName}");
                // foo/bar/prefabs
                var bundleNoExt = bundlePath.Substring(0, bundlePath.Length - BundleFileExt.Length);
                // folderPath: foo/bar
                var folder = Path.GetDirectoryName(bundlePath);

                if (!Directory.Exists(folder)) {
                    continue;
                }

                var files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);

                foreach (var file in files) {
                    var index = file.LastIndexOf(".");

                    if (index == -1) {
                        continue;
                    }

                    // foo/bar/prefabs_a515803f18202a07244ee82b16b6434d
                    var fileNoExt = file.Substring(0, index);

                    if (!fileNoExt.Contains(bundleNoExt)) {
                        continue;
                    }

                    // foo/bar/prefabs.bundle.manifest
                    if (file.EndsWith($"{BundleFileExt}.manifest")) {
                        Debug.Log($"删除旧的资源 {file}");
                        File.Delete(file);
                        continue;
                    }

                    // BuildAssetBundleOptions.AppendHashToAssetBundleName 生成的 bundle 相关文件名会额外带上 "_" + 32 位的 hash
                    if (bundleNoExt.Length == fileNoExt.Length || bundleNoExt.Length + 33 == fileNoExt.Length) {
                        Debug.Log($"删除旧的资源 {file}");
                        File.Delete(file);
                    }
                }
            }

            BuildPipeline.BuildAssetBundles(
                outputPath,
                builds,
                options,
                EditorUserBuildSettings.activeBuildTarget
            );

            GenerateBundlesInfo(pkgHelper, builds, options, targetType, outputPath);
        }

        // assets: { "Samples/CameraStack/Scenes/CameraStack.unity" }
        private static AssetBundleBuild GenerateBundleBuild(string targetPath, string[] assets, string resolvePath) {
            AssetBundleBuild bundleBuild = default;

            if (assets.Length == 0) {
                return bundleBuild;
            }

            string[] addressableNames = new string[assets.Length];

            // rootPath: Assets/Arts/UI
            // assetName: Assets/Arts/UI/Panel/main.prefab
            // addressableName: Panel/main.prefab
            for (int i = 0; i < assets.Length; i++) {
                addressableNames[i] = assets[i].Substring(targetPath.Length + 1);
                assets[i] = $"Assets/{assets[i]}";
            }

            bundleBuild = new AssetBundleBuild {
                assetBundleName = $"{resolvePath}{BundleFileExt}".ToLower(),
                assetNames = assets,
                addressableNames = addressableNames
            };

            return bundleBuild;
        }

        private static void GenerateBundlesInfo(PkgHelper pkgHelper, AssetBundleBuild[] builds, BuildAssetBundleOptions options, string targetType, string outputPath) {
            // 注意：当我们调用 BuildPipeline.BuildAssetBundles() 时，会在 outputPath 生成一份与本级目录名相同的文件来存储 AssetBundleManifest，我们需要通过它来获取 bundles 的相关信息，并生成对应的 xxsx.assets、xxx.deps 文件。
            // example:
            // outputPath : foo/bar/sandbox
            // src : foo/bar/sandbox/sandbox
            var src = Path.Combine(outputPath, Path.GetFileName(outputPath));

            if (!File.Exists(src)) {
                BuildCacheData.IsError = true;
                Debug.LogError("构建出错，未成功生成 manifest 文件");
                return;
            }

            var manifest = AssetBundle.LoadFromFile(src).LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            var allAssetBundles = manifest.GetAllAssetBundles();

            foreach (var bundle in allAssetBundles) {
                var dependencies = manifest.GetDirectDependencies(bundle);
                // bundle: foo/bar/prefabs_a515803f18202a07244ee82b16b6434d.bundle
                // bundleHash: a515803f18202a07244ee82b16b6434d
                var bundleHash = manifest.GetAssetBundleHash(bundle).ToString();
                var bundleNoHash = bundle;

                if (options.HasFlag(BuildAssetBundleOptions.AppendHashToAssetBundleName)) {
                    bundleNoHash = $"{bundle.Substring(0, bundle.Length - bundleHash.Length - 1 - BundleFileExt.Length)}{BundleFileExt}";
                }

                var targetPath = bundleNoHash.Replace(".bundle", "");

                // 存储构建的 .bundle 文件
                BuildCacheData.AddFileToBuildTarget(targetPath, targetType, bundle);

                // 1. 生成 xxx.assets
                foreach (var build in builds) {
                    // assetBundleName: foo/bar/prefabs.bundle
                    var buildBundleName = build.assetBundleName;

                    // BuildAssetBundleOptions.AppendHashToAssetBundleName: 生成的 bundleName 会额外带上 "_" + 32 位的 hash
                    if (options.HasFlag(BuildAssetBundleOptions.AppendHashToAssetBundleName)) {
                        // bundleNameNoExt: foo/bar/prefabs
                        var bundleNameNoExt = buildBundleName.Substring(0, build.assetBundleName.Length - BundleFileExt.Length);
                        // bundleName: foo/bar/prefabs_a515803f18202a07244ee82b16b6434d.bundle
                        buildBundleName = $"{bundleNameNoExt}_{bundleHash}{BundleFileExt}";
                    }

                    if (buildBundleName.Equals(bundle)) {
                        var assetsPath = $"{outputPath}/{Path.ChangeExtension(bundle, BundleAssetsFileExt)}".ToLower();
                        File.WriteAllText(assetsPath, $"{string.Join("\n", build.addressableNames).ToLower()}\n");

                        // 存储构建的 .asset 文件
                        var assetFile = $"{Path.ChangeExtension(bundle, BundleAssetsFileExt)}";
                        BuildCacheData.AddFileToBuildTarget(targetPath, targetType, assetFile);
                        break;
                    }
                }

                if (dependencies == null || dependencies.Length == 0) {
                    continue;
                }

                // 2. 生成 xxx.deps
                if (options.HasFlag(BuildAssetBundleOptions.AppendHashToAssetBundleName)) {
                    for (var i = 0; i < dependencies.Length; i++) {
                        // deps[i]: foo/bar/prefabs_a515803f18202a07244ee82b16b6434d.bundle
                        // depNoExt: foo/bar/prefabs
                        var depNoExt = $"{dependencies[i].Substring(0, dependencies[i].Length - bundleHash.Length - 1 - BundleFileExt.Length)}";
                        // deps[i]: foo/bar/prefabs.bundle
                        dependencies[i] = $"{depNoExt}{BundleFileExt}";
                    }
                }

                // bundle : foo/bar/arts/prefabs.bundle
                // depPath : e:/foobar/foo/bar/arts/prefabs.deps
                var relativePath = Path.ChangeExtension(bundle, BundleDepsFileExt);
                var depPath = $"{outputPath}/{relativePath}".ToLower();

                // 结尾必须加 \n 是与我们 AssetDB.cs 中的 AddDeps 解析逻辑有关
                File.WriteAllText(depPath, $"{string.Join("\n", dependencies)}\n");

                // 存储构建的 .deps 文件
                BuildCacheData.AddFileToBuildTarget(targetPath, targetType, relativePath);
            }

            File.Delete(src);
        }


        private static void BuildSubScenes(PkgHelper pkgHelper, string targetPath, string outputPath) {
#if USE_ENTITIES
            var target = new BuildCacheTarget {
                path = targetPath,
                type = PkgTargets[1],
                files = new List<string> {},
            };

            BuildCacheData.AddBuildTarget(target);

            string[] matchedFiles = pkgHelper.GetTargetAssets(targetPath, PkgTargets[1]);

            if (matchedFiles.Length == 0) {
                Debug.LogWarning($"该 {targetPath} 未匹配到任何资源, 请检查 patterns 配置!");
                return;
            }

            for (int i = 0; i < matchedFiles.Length; i++) {
                matchedFiles[i] = $"Assets/{matchedFiles[i]}";
            }

            // 以本身的 Path 作为输出路径
            // outputPath : foo/bar/sandbox
            // targetPath : Samples/CameraStack/Scenes
            // output : foo/bar/samples/camerastack/scenes

            var output = $"{outputPath}/{targetPath}".ToLower();
            var contentArchivesOutPut = $"{output}/content_archives";
            var entityScenesOutPut = $"{output}/entityscenes";

            try {
                if (Directory.Exists(contentArchivesOutPut)) {
                    Directory.Delete(contentArchivesOutPut, true);
                }

                if (Directory.Exists(entityScenesOutPut)) {
                    Directory.Delete(entityScenesOutPut, true);
                }
            } catch (Exception e) {
                throw e;
            }

            var sceneGuids = new string[matchedFiles.Length];

            for (var i = 0; i < matchedFiles.Length; i++) {
                var matchedFile = matchedFiles[i];
                var guid = AssetDatabase.AssetPathToGUID(matchedFile);
                sceneGuids[i] = guid;
            }

            var subSceneGuids = new HashSet<Hash128>(sceneGuids.SelectMany(sceneGuid =>
                EditorEntityScenesPublic.GetSubScenes(new GUID(sceneGuid))));
            var artifactKeys = new Dictionary<Hash128, ArtifactKey>();
            EntitySceneBuildUtilityPublic.PrepareEntityBinaryArtifacts(default, subSceneGuids, artifactKeys);
            EntitySceneBuildUtilityPublic.PrepareAdditionalFiles(
                artifactKeys.Keys.ToArray(),
                artifactKeys.Values.ToArray(),
                EditorUserBuildSettings.activeBuildTarget,
                (from, to) => {
                    var targetFile = $"{targetPath}/{to}";
                    to = $"{output}/{to}";
                    var parent = Path.GetDirectoryName(to);
                    Directory.CreateDirectory(parent);
                    File.Copy(from, to, true);

                    BuildCacheData.AddFileToBuildTarget(targetPath, PkgTargets[1], targetFile);
                });
#endif
        }

        private static void CopyConfigs(PkgHelper pkgHelper, string targetPath, string outputPath) {
            string[] matchedFiles = pkgHelper.GetTargetAssets(targetPath, PkgTargets[2]);

            if (matchedFiles.Length == 0) {
                Debug.LogWarning($"该 {targetPath} 未匹配到任何资源, 请检查 patterns 配置!");
                return;
            }

            var target = new BuildCacheTarget {
                path = targetPath,
                type = PkgTargets[2],
                files = new List<string> { },
            };

            BuildCacheData.AddBuildTarget(target);

            var caseSensitive = pkgHelper.IsTargetCaseSensitive(targetPath, PkgTargets[2]);

            foreach (var matchedFile in matchedFiles) {
                var destFile = matchedFile;
                var destFullFile = $"{outputPath}/{destFile}";

                // 若大小写不敏感，则小写
                if (!caseSensitive) {
                    destFullFile = destFullFile.ToLower();
                    destFile = destFile.ToLower();
                }

                var destDirectory = Path.GetDirectoryName(destFullFile);

                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy($"Assets/{matchedFile}", destFullFile, true);
                BuildCacheData.AddFileToBuildTarget(targetPath, PkgTargets[2], destFile);
            }
        }

        private static void BuildDlls(PkgHelper pkgHelper, string targetPath, string outputPath) {
            // 将构建的生成文件路径加入 target
            var target = new BuildCacheTarget {
                path = targetPath,
                type = PkgTargets[3],
                files = new List<string> { },
            };

            BuildCacheData.AddBuildTarget(target);
            string[] matchedFiles = pkgHelper.GetTargetAssets(targetPath, PkgTargets[3]);

            if (matchedFiles.Length == 0) {
                Debug.LogWarning($"该 {targetPath} 未匹配到任何资源, 请检查 patterns 配置!");
                return;
            }

            for (int i = 0; i < matchedFiles.Length; i++) {
                matchedFiles[i] = $"Assets/{matchedFiles[i]}";
            }

            int length = matchedFiles.Length;
            string[] srcPaths = new string[length];
            string[] destPaths = new string[length];

            for (int i = 0; i < matchedFiles.Length; i++) {
                // matchedFile : Assets/Scripts/Core/Runtime/Framework.Core.Runtime.asmdef
                string matchedFile = matchedFiles[i];
                // 暂时改成直接拷贝。因为 reimport 后会触发 unity compile, 与此同时构建 runtime 会提示 Building is not allowed while Unity is compiling
                // AssetDatabase.ImportAsset(matchedFile, ImportAssetOptions.ForceSynchronousImport);
                AssemblyDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(matchedFile);
                string dllName = $"{asset.name}.dll";
                var srcFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dllName}";

                // relativeDirectory : Scripts/Core/Runtime
                string relativeDirectory = Path.GetDirectoryName(matchedFile.Substring(7));

                var destFile = $"{outputPath}/{relativeDirectory}/{dllName}".ToLower();
                var destDirectory = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory);
                }

                srcPaths[i] = srcFile;
                destPaths[i] = destFile;
            }

            for (int j = 0; j < length; j++) {
                File.Copy(srcPaths[j], destPaths[j], true);
            }

            for (int j = 0; j < length; j++) {
                var path = destPaths[j].Replace($"{outputPath.ToLower()}/", "");
                BuildCacheData.AddFileToBuildTarget(targetPath, PkgTargets[3], path.Replace("\\", "/"));
            }
        }

        private static void ClearManifests(string outputPath) {
            if (!Directory.Exists(outputPath)) {
                return;
            }

            var files = Directory.GetFiles(outputPath, "*.manifest", SearchOption.AllDirectories);

            foreach (var file in files) {
                File.Delete(file);
            }
        }
    }
}