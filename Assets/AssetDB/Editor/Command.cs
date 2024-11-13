
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FallShadow.Asset.Editor {
    public class BuildEvents {
        public static event Action OnBeforeBuild;
        public static event Action OnAfterBuild;

        public static void ClearEvent() {
            OnBeforeBuild = null;
            OnAfterBuild = null;
        }

        internal static void TriggerBeforeBuild() {
            OnBeforeBuild?.Invoke();
        }

        internal static void TriggerAfterBuild() {
            OnAfterBuild?.Invoke();
        }
    }

    public static class Command {

        public static void BuildSelectAssets(string[] selectAssetPaths, bool clearManifest, string output, string[] ignorePatterns = null) {
            var platform = GetActivePlatform();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var pkgHelper = new PkgHelper(Application.dataPath, platform, ignorePatterns);
            stopwatch.Stop();
            UnityEngine.Debug.LogWarning($"forge_pkg init cost(s): {stopwatch.Elapsed.TotalSeconds}");

            BuildCacheData.BeginBuild();
            BuildSelectAssets(pkgHelper, selectAssetPaths, clearManifest, output);
            pkgHelper.Dispose();
            BuildCacheData.EndBuild();
        }

        public static string GetActivePlatform() {
            switch (EditorUserBuildSettings.activeBuildTarget) {
                case UnityEditor.BuildTarget.StandaloneLinux64:
                    return "linux";

                case UnityEditor.BuildTarget.StandaloneOSX:
                    return "mac";

                case UnityEditor.BuildTarget.Android:
                    return "android";

                case UnityEditor.BuildTarget.iOS:
                    return "ios";

                case UnityEditor.BuildTarget.WebGL:
                    return "webgl";

#if TUANJIE_2022_3_OR_NEWER
                case UnityEditor.BuildTarget.WeixinMiniGame:
                    return "minigame";
#endif

                default:
                    return "windows";
            }
        }

        /// <summary>
        /// 根据选中目录，构建对应的资源
        /// </summary>
        internal static void BuildSelectAssets(PkgHelper pkgHelper, string[] selectAssetPaths, bool clearManifest, string output) {
            try {
                BuildEvents.TriggerBeforeBuild();
                pkgHelper.PrintDebugInfo();

                for (int i = 0; i < selectAssetPaths.Length; i++) {
                    selectAssetPaths[i] = Path.GetFullPath(selectAssetPaths[i]).Replace("\\", "/");
                }

                // Build Normal
                var buildCtxPtr = pkgHelper.BuildCtxInitialize(selectAssetPaths);
                var pkgPaths = pkgHelper.GetBuildCtxPkgFiles(buildCtxPtr);

                foreach (var pkgPath in pkgPaths) {
                    Builder.Exec(pkgHelper, pkgPath, clearManifest, output);
                }

                pkgHelper.BuildCtxDispose(buildCtxPtr);

                // Build TargetPath
                foreach (var selectAssetPath in selectAssetPaths) {
                    // 忽略文件及选中 Assets 目录
                    if (File.Exists(selectAssetPath) || Application.dataPath.Length >= selectAssetPath.Length) {
                        continue;
                    }

                    // 获取 pkgPath
                    var pkgPath = pkgHelper.GetOuterPkgFile(selectAssetPath);

                    if (string.IsNullOrEmpty(pkgPath) || pkgPaths.Contains(pkgPath)) {
                        continue;
                    }

                    var relativePath = selectAssetPath.Substring(Application.dataPath.Length + 1);
                    var targetTypes = pkgHelper.GetTargetTypes(pkgPath);

                    foreach (var targetType in targetTypes) {
                        var targetPaths = pkgHelper.GetTargetPaths(pkgPath, targetType);

                        if (!targetPaths.Contains(relativePath)) {
                            continue;
                        }

                        UnityEngine.Debug.Log($"开始构建 {targetType}: {relativePath}");
                        Builder.Exec(pkgHelper, relativePath, targetType, true, output);
                        UnityEngine.Debug.Log($"成功构建 {targetType}: {relativePath}");
                    }
                }

                BuildEvents.TriggerAfterBuild();
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError(e.StackTrace);
                throw;
            }
        }

        public static void GenerateFileList(string buildPath, string[] regex = null) {
            string[] matchRegex = { "**/*", "!**/*.meta" };

            if (regex != null) {
                matchRegex = matchRegex.Concat(regex).ToArray();
            }

            var assetPaths = PkgCS.MatchRelativeFile(buildPath, matchRegex);
            ProcessFileList(buildPath, assetPaths.ToList());
        }

        private static void ProcessFileList(string buildPath, List<string> assetPaths) {
            var bundle2Assets = new Dictionary<string, string[]>();

            for (var i = 0; i < assetPaths.Count; i++) {
                var assetPath = assetPaths[i];

                if (!assetPath.EndsWith(Builder.BundleAssetsFileExt)) {
                    continue;
                }

                // 读取 assets
                var filePath = $"{buildPath}/{assetPath}";

                if (!File.Exists(filePath)) {
                    UnityEngine.Debug.LogError($"不存在 {filePath}, 请检查");
                    return;
                }

                assetPaths.RemoveAt(i);
                i--;

                var bundleKey = Path.ChangeExtension(assetPath, Builder.BundleFileExt);
                var assetLines = File.ReadAllLines(filePath);
                bundle2Assets.Add(bundleKey, assetLines);
            }

            for (var i = 0; i < assetPaths.Count; i++) {
                var assetPath = assetPaths[i];

                if (!bundle2Assets.TryGetValue(assetPaths[i], out var assets)) {
                    continue;
                }

                foreach (var asset in assets) {
                    assetPath = $"{assetPath}\n\t{asset}";
                }

                assetPaths[i] = assetPath;
            }

            File.WriteAllText($"{buildPath}/files.txt", $"{string.Join("\n", assetPaths)}\n");
        }
    }
}