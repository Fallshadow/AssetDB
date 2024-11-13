using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Framework.Shares {
    public class RustPtrNullException : Exception { }

    internal static class RustPtrUtil {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValid(IntPtr ptr) {
            return ptr != default && ptr != IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckAndThrow(IntPtr ptr) {
            if (IsValid(ptr)) {
                return;
            }

            Debug.LogError("rust Ö¸ÕëÎª¿Õ");
            throw new RustPtrNullException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckStringAndThrow(string str) {
            if (string.IsNullOrEmpty(str)) {
                throw new NullReferenceException("×Ö·û´®Îª¿Õ");
            }
        }
    }
}
