using UnityEditor.PackageManager;
using UnityEditor;
using System.Collections.Generic;

namespace FallShadow.Asset.Editor {
    internal struct BuildCacheTarget {
        public string path;
        public string type;
        public List<string> files;
        public List<string> depends;
        public string packedHash;
    }

    public class BuildCacheData {
        private static bool _isBuild;
        internal static bool IsError { get; set; }
        internal static List<BuildCacheTarget> buildTargets = new();
        internal static void BeginBuild() {
            _isBuild = true;
            IsError = false;
            buildTargets.Clear();
        }

        internal static void EndBuild() {
            _isBuild = false;
            IsError = false;
            buildTargets.Clear();
        }

        internal static void AddBuildTarget(BuildCacheTarget target) {
            var idx = GetBuildTargetIndex(target.path, target.type);

            if (idx != -1) {
                return;
            }

            target.depends ??= new List<string>();
            buildTargets.Add(target);
        }

        public static int GetBuildTargetIndex(string path, string type) {
            var count = buildTargets.Count;
            var lowerPath = path.ToLower();

            for (var i = 0; i < count; i++) {
                var target = buildTargets[i];
                var tempPath = target.path.ToLower();

                if (tempPath.Equals(lowerPath) && target.type.Equals(type)) {
                    return i;
                }
            }

            return -1;
        }

        public static void AddFileToBuildTarget(string path, string type, string file) {
            var lowerPath = path.ToLower();

            foreach (var target in buildTargets) {
                var tempPath = target.path.ToLower();

                if (!tempPath.Equals(lowerPath) || !target.type.Equals(type)) {
                    continue;
                }

                if (!target.files.Contains(file)) {
                    target.files.Add(file);
                }
            }
        }
    }
}