using System;
using System.Runtime.InteropServices;

namespace Framework.Shares {
    public class TomlFFI {

#if !UNITY_EDITOR && UNITY_IPHONE
        const string dllName = "__Internal";
#else
        const string dllName = "froge_toml";
#endif
        // ===============================================
        // Info
        // ===============================================
        [DllImport(dllName)]
        public static extern IntPtr ftoml_version();

        /// ===============================================
        /// Document in toml
        /// ===============================================

        // return Document ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_parse_file([MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        // return Document ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_parse_content([MarshalAs(UnmanagedType.LPUTF8Str)] string url);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_get(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_as_item(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_as_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void ftoml_document_dispose(IntPtr ptr);

        /// ===============================================
        /// Item in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern bool ftoml_item_is_value(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_value(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_array_of_tables(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_array_of_tables(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_none(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_integer(IntPtr ptr);

        [DllImport(dllName)]
        public static extern int ftoml_item_as_int32(IntPtr ptr);

        [DllImport(dllName)]
        public static extern long ftoml_item_as_int64(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_float(IntPtr ptr);

        [DllImport(dllName)]
        public static extern float ftoml_item_as_float(IntPtr ptr);

        [DllImport(dllName)]
        public static extern double ftoml_item_as_double(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_bool(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_as_bool(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_str(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_str(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_array(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_array(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_item_is_inline_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_as_inline_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void ftoml_item_dispose(IntPtr ptr);

        /// ===============================================
        /// Value in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_type_name(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_integer(IntPtr ptr);

        [DllImport(dllName)]
        public static extern int ftoml_value_as_int32(IntPtr ptr);

        [DllImport(dllName)]
        public static extern long ftoml_value_as_int64(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_float(IntPtr ptr);

        [DllImport(dllName)]
        public static extern float ftoml_value_as_float(IntPtr ptr);

        [DllImport(dllName)]
        public static extern double ftoml_value_as_double(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_bool(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_as_bool(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_str(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_as_str(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_array(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_as_array(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_value_is_inline_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_as_inline_table(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void ftoml_value_dispose(IntPtr ptr);

        /// ===============================================
        /// Array in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern bool ftoml_array_is_empty(IntPtr ptr);

        [DllImport(dllName)]
        public static extern uint ftoml_array_len(IntPtr ptr);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_array_get(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void ftoml_array_dispose(IntPtr ptr);

        /// ===============================================
        /// Table in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern bool ftoml_table_is_empty(IntPtr ptr);

        [DllImport(dllName)]
        public static extern uint ftoml_table_len(IntPtr ptr);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_get(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern bool ftoml_table_contains_key(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern bool ftoml_table_contains_table(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern bool ftoml_table_contains_value(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern bool ftoml_table_contains_array_of_tables(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern void ftoml_table_dispose(IntPtr ptr);

        /// ===============================================
        /// InlineTable in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern bool ftoml_inline_table_is_empty(IntPtr ptr);

        [DllImport(dllName)]
        public static extern uint ftoml_inline_table_len(IntPtr ptr);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_inline_table_get(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_inline_table_get_keys(IntPtr ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_inline_table_contains_key(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern void ftoml_inline_table_dispose(IntPtr ptr);

        /// ===============================================
        /// ArrayOfTables in toml
        /// ===============================================
        [DllImport(dllName)]
        public static extern bool ftoml_table_array_is_empty(IntPtr ptr);

        [DllImport(dllName)]
        public static extern uint ftoml_table_array_len(IntPtr ptr);

        // return Table ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_array_get(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void ftoml_table_array_dispose(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_get_keys(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_get_table_keys(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_get_keys(IntPtr ptr);

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_get_inline_table_keys(IntPtr ptr);

        // return Vec<String> ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_get_table_array_keys(IntPtr ptr);

        /// ===============================================
        /// String array in toml
        /// ===============================================

        [DllImport(dllName)]
        public static extern uint ftoml_strs_len(IntPtr ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_strs_get(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void ftoml_strs_dispose(IntPtr ptr);

        /// ===============================================
        /// new document
        /// ===============================================
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_new();

        // item_ptr: Item ptr
        [DllImport(dllName)]
        public static extern bool ftoml_document_insert(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr item_ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_document_remove(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern void ftoml_document_clear(IntPtr ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_document_to_string(IntPtr ptr);

        /// ===============================================
        /// new item toml
        /// ===============================================
        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_i32(int val);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_i64(long val);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_float(float val);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_double(double val);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_bool(bool val);

        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_str([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        // value_ptr: Value ptr
        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_value(IntPtr value_ptr);

        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_table_array(IntPtr table_array_ptr);

        // inline_table_ptr: InlineTable ptr
        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_inline_table(IntPtr inline_table_ptr);

        // table_ptr: Table ptr
        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_table(IntPtr table_ptr);

        // array_ptr: Array ptr
        // return Item ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_from_array(IntPtr arrar_ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_item_to_string(IntPtr ptr);

        /// ===============================================
        /// write value
        /// ===============================================
        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_i32(int val);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_i64(long val);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_float(float val);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_double(double val);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_bool(bool val);

        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_str([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        // item_ptr: Item ptr
        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_item(IntPtr item_ptr);

        // inline_table_ptr: InlineTable ptr
        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_inline_table(IntPtr inline_table_ptr);

        // array_ptr: Array ptr
        // return Value ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_from_array(IntPtr arrar_ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_value_to_string(IntPtr ptr);

        /// ===============================================
        /// write array
        /// ===============================================
        // return Array ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_array_new();

        // value_ptr: Value ptr
        [DllImport(dllName)]
        public static extern void ftoml_array_push(IntPtr ptr, IntPtr value_ptr);

        // value_ptr: Value ptr
        [DllImport(dllName)]
        public static extern void ftoml_array_insert(IntPtr ptr, uint index, IntPtr value_ptr);

        // value_ptr: Value ptr
        [DllImport(dllName)]
        public static extern void ftoml_array_replace(IntPtr ptr, uint index, IntPtr value_ptr);

        [DllImport(dllName)]
        public static extern void ftoml_array_remove(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void ftoml_array_clear(IntPtr ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_array_to_string(IntPtr ptr);

        [DllImport(dllName)]
        public static extern void ftoml_array_pretty(IntPtr ptr);

        /// ===============================================
        /// write table
        /// ===============================================
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_new();

        // item_ptr: Item ptr
        [DllImport(dllName)]
        public static extern bool ftoml_table_insert(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr item_ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_table_remove(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern void ftoml_table_clear(IntPtr ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_to_string(IntPtr ptr);

        /// ===============================================
        /// write inline table
        /// ===============================================
        // return InlineTable ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_inline_table_new();

        // value_ptr: Value ptr
        [DllImport(dllName)]
        public static extern bool ftoml_inline_table_insert(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr value_ptr);

        [DllImport(dllName)]
        public static extern bool ftoml_inline_table_remove(IntPtr ptr, [MarshalAs(UnmanagedType.LPUTF8Str)] string key);

        [DllImport(dllName)]
        public static extern void ftoml_inline_table_clear(IntPtr ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_inline_table_to_string(IntPtr ptr);

        /// ===============================================
        /// write table array
        /// ===============================================

        // return ArrayOfTables ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_array_new();

        // table_ptr: Table ptr
        [DllImport(dllName)]
        public static extern void ftoml_table_array_push(IntPtr ptr, IntPtr table_ptr);

        [DllImport(dllName)]
        public static extern void ftoml_table_array_remove(IntPtr ptr, uint index);

        [DllImport(dllName)]
        public static extern void ftoml_table_array_clear(IntPtr ptr);

        // return Array ptr
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_array_to_array(IntPtr ptr);

        // retrun *const c_char
        // convert to string by using Marshal.PtrToStringUTF8(ptr)
        [DllImport(dllName)]
        public static extern IntPtr ftoml_table_array_to_string(IntPtr ptr);

        // ptr: c_char ptr
        [DllImport(dllName)]
        public static extern void ftoml_str_dispose(IntPtr ptr);

        public static string Ptr2String(IntPtr ptr) {
            string str = Marshal.PtrToStringUTF8(ptr);
            ftoml_str_dispose(ptr);
            return str;
        }
    }
}
