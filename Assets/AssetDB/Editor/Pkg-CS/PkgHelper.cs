using System;
using UnityEngine;

namespace FallShadow.Asset.Editor {

    public class PkgHelper {
        private const string BuildWorkspaceDesc = "BuildWorkspace";
        private const string BuildCtxDesc = "BuildCtx";
        private const string CircleDepDesc = "存在循环依赖, 文件列表";

        private IntPtr workspacePtr;

        public PkgHelper(string rootPath, string platform, string[] patterns = null) {
            PkgFFI.fpkg_version();
            var builderPtr = PkgFFI.fpkg_ws_builder_new(rootPath, platform);
            PkgCS.PkgCheckAndThrow(builderPtr);
            patterns ??= new string[] { };
            PkgFFI.fpkg_ws_builder_set_ignore_patterns(builderPtr, patterns, (uint)patterns.Length);
            PkgFFI.fpkg_ws_builder_set_rgignore_file(builderPtr, $"{Builder.PkgflowPath}/.ignore");
            workspacePtr = PkgFFI.fpkg_ws_builder_build(builderPtr);
            PkgCS.PkgCheckAndThrow(builderPtr);
        }

        public void PrintDebugInfo() {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            Debug.Log($"{BuildWorkspaceDesc}: {PkgCS.PtrToString(PkgFFI.fpkg_ws_to_string(workspacePtr))}");
        }

        public void PrintBuildCtxDebugInfo(IntPtr buildCtxPtr) {
            PkgCS.PkgCheckAndThrow(buildCtxPtr);
            Debug.Log($"{BuildCtxDesc}: {PkgCS.PtrToString(PkgFFI.fpkg_bcx_to_string(buildCtxPtr))}");
        }

        public IntPtr BuildCtxInitialize(string[] selectPaths) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr buildCtxPtr = PkgFFI.fpkg_bcx_new(workspacePtr, selectPaths, (uint)selectPaths.Length);
            PrintBuildCtxDebugInfo(buildCtxPtr);
            return buildCtxPtr;
        }

        public string[] GetBuildCtxPkgFiles(IntPtr buildCtxPtr) {
            PkgCS.PkgCheckAndThrow(buildCtxPtr);
            IntPtr ptr = PkgFFI.fpkg_bcx_get_pkgfiles(buildCtxPtr);
            return PkgCS.IntPtr2StringList(ptr);
        }

        public string[] GetTargetTypes(string pkgPath) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr targetTypesPtr = PkgFFI.fpkg_ws_get_target_types_in_pkgfile(workspacePtr, pkgPath);
            PkgCS.PkgCheckAndThrow(targetTypesPtr);
            return PkgCS.IntPtr2StringList(targetTypesPtr);
        }

        public string[] GetTargetPaths(string pkgPath, string targetType) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr pathsPtr = PkgFFI.fpkg_ws_get_target_paths_in_pkgfile(workspacePtr, pkgPath, targetType);
            PkgCS.PkgCheckAndThrow(pathsPtr);
            return PkgCS.IntPtr2StringList(pathsPtr);
        }

        public string[] GetTargetAssets(string targetPath, string targetType) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr assetsPtr = PkgFFI.fpkg_ws_get_target_asset_paths(workspacePtr, targetPath, targetType);
            return PkgCS.IntPtr2StringList(assetsPtr);
        }

        public string[] GetBundleDeps(string targetPath) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr depsPtr = PkgFFI.fpkg_ws_resolve_bundle_deps(workspacePtr, targetPath);

            if (depsPtr == IntPtr.Zero) {
                return new string[] { };
            }

            IntPtr filePtr = PkgFFI.fpkg_deps_get_targets(depsPtr);
            string[] paths = PkgCS.IntPtr2StringList(filePtr, false);

            if (PkgFFI.fpkg_deps_is_circular(depsPtr)) {
                string message = string.Join("\n\t", paths);

                PkgFFI.fpkg_strs_dispose(filePtr);
                PkgFFI.fpkg_deps_dispose(depsPtr);

                throw new Exception($"{CircleDepDesc}: \n\t{message}");
            }

            PkgFFI.fpkg_strs_dispose(filePtr);
            PkgFFI.fpkg_deps_dispose(depsPtr);
            return paths;
        }

        public bool IsTargetCaseSensitive(string targetPath, string targetType) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            return PkgFFI.fpkg_ws_is_target_case_sensitive(workspacePtr, targetPath, targetType);
        }

        public void BuildCtxDispose(IntPtr buildCtxPtr) {
            PkgCS.PkgCheckAndThrow(buildCtxPtr);
            PkgFFI.fpkg_bcx_dispose(buildCtxPtr);
        }

        public string GetOuterPkgFile(string path) {
            PkgCS.PkgCheckAndThrow(workspacePtr);
            IntPtr pkgPtr = PkgFFI.fpkg_ws_get_outer_pkgfile(workspacePtr, path);
            return PkgCS.IsValid(pkgPtr) ? PkgCS.PtrToString(pkgPtr) : String.Empty;
        }

        public void Dispose() {
            if (!PkgCS.IsValid(workspacePtr)) {
                return;
            }

            PkgFFI.fpkg_ws_dispose(workspacePtr);
            workspacePtr = IntPtr.Zero;
        }
    }
}