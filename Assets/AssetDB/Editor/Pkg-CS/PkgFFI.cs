using System;
using System.Runtime.InteropServices;

namespace FallShadow.Asset.Editor {
    public static class PkgFFI {
        const string dllName = "froge_pkg";
        // ============================================================
        // WorkspaceBuilder api
        // ============================================================
        // return WorkspaceBuilder ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_builder_new(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string root_path,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string platform
        );

        [DllImport(dllName)]
        public static extern void fpkg_ws_builder_set_ignore_patterns(
            IntPtr ptr,
            string[] patterns,
            uint patterns_len
        );

        [DllImport(dllName)]
        public static extern void fpkg_ws_builder_set_rgignore_file(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string file
        );

        [DllImport(dllName)]
        public static extern void fpkg_ws_builder_set_packs_file(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string file
        );

        // return Workspace ptr
        // param ptr will auto dispose
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_builder_build(IntPtr ptr);

        // ============================================================
        // Workspace api
        // ============================================================
        // return Workspace ptr
        [DllImport(dllName)]
        public static extern void fpkg_ws_dispose(IntPtr ptr);

        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_to_string(IntPtr ptr);

        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_root_path(IntPtr ptr);

        // use for other lib, like rpkg-manager
        // return TargetPaths ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_entire_target_paths(IntPtr ptr);

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_target_types_in_pkgfile(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pkg_path
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_target_paths_in_pkgfile(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pkg_path,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_type
        );

        // return Dependencies ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_resolve_bundle_deps(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_path
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_resolve_bundle_used_by(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_path
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_target_asset_paths(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_path,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_type
        );

        [DllImport(dllName)]
        public static extern bool fpkg_ws_is_target_case_sensitive(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_path,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string target_type
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_pack_paths(IntPtr ptr);

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_pack_name(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_path
        );

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_pack_version(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_path
        );

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_pack_path(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_name
        );

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_outer_pack_path(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_name
        );

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_outer_pack_name(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path
        );

        // return c_char ptr, will be null if not found
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_outer_pkgfile(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_pkgfiles_in_pack(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_path,
            bool self_only
        );


        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_gen_pack_asset_urls(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_path,
            bool self_only
        );

        [DllImport(dllName)]
        public static extern bool fpkg_ws_set_pack_version(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string pack_path,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string version
        );

        // ============================================================
        // Dependencies api
        // ============================================================
        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_deps_get_targets(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool fpkg_deps_is_circular(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void fpkg_deps_dispose(IntPtr ptr);
        // ============================================================
        // BuildContext api
        // ============================================================
        // ws_ptr: Workspace ptr
        // build_mode: 0 - normal, 1 - pack
        // return BuildContext ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_bcx_new(
            IntPtr ws_ptr,
            string[] selected_paths,
            uint selected_paths_len
        );

        [DllImport(dllName)]
        public static extern void fpkg_bcx_dispose(IntPtr ptr);

        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_bcx_to_string(IntPtr ptr);

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_bcx_get_pkgfiles(IntPtr ptr);

        // ===============================================
        // Scan api
        // ===============================================
        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_scan_files(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string root_path,
            string[] patterns,
            uint patterns_len
        );

        [DllImport(dllName)]
        public static extern IntPtr fpkg_scan_files_rel_path(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string root_path,
            string[] patterns,
            uint patterns_len
        );

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_scan_files_block_by_pkg(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string root_path,
            string[] patterns,
            uint patterns_len
        );

        // ===============================================
        // Wild api
        // ===============================================
        [DllImport(dllName)]
        public static extern void fpkg_ws_scan_wild_assets(
            IntPtr ptr
        );

        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_wild_assets(
            IntPtr ptr
        );

        [DllImport(dllName)]
        public static extern IntPtr fpkg_ws_get_wild_asset_used_bys(
            IntPtr ptr,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string wild_asset
        );

        // ============================================================
        // misc api
        // ============================================================
        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_version();

        [DllImport(dllName)]
        public static extern bool fpkg_debug_log_init();

        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_try_get_err();

        [DllImport(dllName)]
        public static extern uint fpkg_strs_len(IntPtr ptr);

        // return c_char ptr
        [DllImport(dllName)]
        public static extern IntPtr fpkg_strs_get(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void fpkg_strs_dispose(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void fpkg_str_dispose(IntPtr ptr);

        // ptr: TargetPaths ptr
        [DllImport(dllName)]
        public static extern void fpkg_target_paths_dispose(IntPtr ptr);
    }
}
