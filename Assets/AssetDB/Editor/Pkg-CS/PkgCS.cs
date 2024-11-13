using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
namespace FallShadow.Asset.Editor {
    public class RustPtrNullException : Exception { }

    public class PkgCS {
        private const string NullPointerDesc = "pkg => rust Ö¸ÕëÎª¿Õ!";

        public static void PkgCheckAndThrow(IntPtr ptr) {
            if (ptr != default && ptr != IntPtr.Zero) {
                return;
            }

            Debug.LogError($"{NullPointerDesc} error: {TryGetError()}");
            throw new RustPtrNullException();
        }

        public static string TryGetError() {
            return PtrToString(PkgFFI.fpkg_try_get_err());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string PtrToString(IntPtr ptr) {
            PkgCheckAndThrow(ptr);
            string str = Marshal.PtrToStringUTF8(ptr);
            PkgFFI.fpkg_str_dispose(ptr);
            return str;
        }

        public static string[] IntPtr2StringList(IntPtr ptr, bool isDispose = true) {
            PkgCheckAndThrow(ptr);

            uint len = PkgFFI.fpkg_strs_len(ptr);
            string[] strs = new string[len];

            for (int i = 0; i < len; i++) {
                string str = PtrToString(PkgFFI.fpkg_strs_get(ptr, (uint)i));

                if (string.IsNullOrEmpty(str)) {
                    str = "";
                }

                strs[i] = str;
            }

            if (!isDispose) {
                return strs;
            }

            PkgFFI.fpkg_strs_dispose(ptr);
            return strs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(IntPtr ptr) {
            return ptr != default && ptr != IntPtr.Zero;
        }

        public static string[] MatchRelativeFile(string rootPath, string[] patterns) {
            IntPtr strPtr = PkgFFI.fpkg_scan_files_rel_path(rootPath, patterns, (UInt32)patterns.Length);
            return IntPtr2StringList(strPtr);
        }
    }
}
