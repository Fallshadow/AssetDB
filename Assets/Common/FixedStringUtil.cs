using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

// [MethodImpl(MethodImplOptions.AggressiveInlining)] 方法实现属性，提示编译器尽可能地内联这个方法。内联可以减少方法调用的开销，提高性能，特别是在频繁调用的小方法中

namespace FallShadow.Common {
    public static class FixedStringUtil {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes Replace(FixedString512Bytes input, char oldValue, char newValue) {
            unsafe {
                var ptr = input.GetUnsafePtr();

                for(var i = 0; i < input.Length; i++) {
                    if(ptr[i] == (byte)oldValue) {
                        ptr[i] = (byte)newValue;
                    }
                }

                return input;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes Substring(FixedString512Bytes fs, int startIndex, int length) {
            if (startIndex + length > fs.Length) {
                throw new ArgumentOutOfRangeException();
            }

            unsafe {
                return GetFixedString512(fs.GetUnsafePtr(), startIndex, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes Substring(FixedString512Bytes fs, int startIndex) {
            if (startIndex >= fs.Length) {
                throw new ArgumentOutOfRangeException();
            }

            unsafe {
                return GetFixedString512(fs.GetUnsafePtr(), startIndex, fs.Length - startIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe FixedString512Bytes GetFixedString512(byte* p, int from, int length) {
            FixedString512Bytes result = default;
            UTF8ArrayUnsafeUtility.Copy(result.GetUnsafePtr(), out var len, FixedString512Bytes.UTF8MaxLengthInBytes, p + from, length);
            result.Length = len;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes ToLower(FixedString512Bytes input) {
            unsafe {
                var ptr = input.GetUnsafePtr();

                for (var i = 0; i < input.Length; i++) {
                    var value = ptr[i];

                    if (value >= 65 && value <= 90) {
                        ptr[i] = (byte)(value + 32);
                    }
                }

                return input;
            }
        }
    }
}