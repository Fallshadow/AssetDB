using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Shares {
    public enum TomlFieldType {
        None,
        Array,
        Bool,
        Float,
        String,
        Integer,
        ArrayOfTable,
        InlineTable,
        Table,
    }

    public static class TomlCS {
        public static TomlDocument Parse(string content) {
            IntPtr doc = IntPtr.Zero;

            if (content == null) {
                Debug.LogError("toml content is null.");
                return default;
            }

            try {
                doc = TomlFFI.ftoml_document_parse_content(content);
                RustPtrUtil.CheckAndThrow(doc);
                return new TomlDocument(doc);
            }
            catch (RustPtrNullException e) {
                Debug.LogError($"invalid toml content: {content}. error: {e.StackTrace}");
                TomlFFI.ftoml_document_dispose(doc);
            }

            return default;
        }

        public static TomlDocument CreateTomlDocument() {
            var docPtr = TomlFFI.ftoml_document_new();
            return new TomlDocument(docPtr);
        }

        public static ITomlTable CreateTable(bool isInline) {
            // 加入到 document 的时候需要转换并销毁该指针
            if (isInline) {
                var ptr = TomlFFI.ftoml_inline_table_new();
                var table = new InlineTomlTable(ptr);
                return table;
            }
            else {
                var ptr = TomlFFI.ftoml_table_new();
                var table = new NormalTomlTable(ptr);
                return table;
            }
        }
    }

    public static class TomlDeserialization {
        public delegate T Deserialize<out T>(ITomlTable value, string key);

        private static readonly Dictionary<Type, Delegate> deserializers;

        static TomlDeserialization() {
            deserializers = new Dictionary<Type, Delegate>();

            RegisterMapper<string[]>(((table, key) => {
                if (table.GetArrayValueType(key) != TomlFieldType.String) {
                    throw new TomlTypeMismatchException(TomlFieldType.Array, table.GetType(key),
                        typeof(string[]));
                }

                return table.GetStringArray(key);
            }));
        }

        public static void RegisterMapper<T>(Deserialize<T> deserializer) {
            if (deserializer != null) {
                deserializers[typeof(T)] = new Deserialize<object>(BoxedDeserializer);

                object BoxedDeserializer(ITomlTable value, string k) {
                    T val = deserializer(value, k);

                    if (val == null) {
                        Debug.LogError($"when deserializing {k}, deserializer returned null for type {typeof(T)}");
                        return null;
                    }

                    return val;
                }
            }
        }

        public static T To<T>(string text) {
            using var doc = TomlCS.Parse(text);
            return To<T>(doc, null);
        }

        public static T To<T>(ITomlTable table, string key) {
            if (deserializers.TryGetValue(typeof(T), out var value)) {
                var deserialize = (Deserialize<object>)value;
                return (T)deserialize(table, key);
            }

            return default;
        }

        public static T To<T>(TomlDocument document, string key) {
            if (deserializers.TryGetValue(typeof(T), out var value)) {
                var deserialize = (Deserialize<object>)value;
                return (T)deserialize(document.GetTable(), key);
            }

            return default;
        }
    }

    public class TomlTypeMismatchException : Exception {
        private readonly string expectedTypeName;
        private readonly string actualTypeName;
        private readonly Type context;

        public TomlTypeMismatchException(TomlFieldType expected, TomlFieldType actual, Type context) {
            this.expectedTypeName = expected.ToString();
            this.actualTypeName = actual.ToString();
            this.context = context;
        }

        public TomlTypeMismatchException(Type expected, TomlFieldType actual, Type context) {
            this.expectedTypeName = expected.FullName;
            this.actualTypeName = actual.ToString();
            this.context = context;
        }

        public override string Message => string.Format("While trying to convert to type {0}, a TOML value of type {1} was required but a value of type {2} was found", (object)this.context, (object)this.expectedTypeName, (object)this.actualTypeName);
    }

    public struct TomlDocument : IDisposable {
        private IntPtr ptr;
        private readonly NormalTomlTable globalTable;

        public TomlDocument(IntPtr ptr) {
            this.ptr = ptr;
            globalTable = new NormalTomlTable(TomlFFI.ftoml_document_as_table(ptr));
        }

        public ITomlTable GetTable() {
            return globalTable;
        }

        public void Dispose() {
            if (ptr != IntPtr.Zero) {
                TomlFFI.ftoml_document_dispose(ptr);
                globalTable.Dispose();
                ptr = IntPtr.Zero;
            }
        }

        public bool Has(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            return globalTable.Has(key);
        }

        public string[] GetKeys() {
            return ptr == IntPtr.Zero ? null : globalTable.GetKeys();
        }

        public TomlFieldType GetType(string key) {
            return ptr == IntPtr.Zero ? TomlFieldType.None : globalTable.GetType(key);
        }

        public ITomlTable GetTable(string key) {
            if (ptr != IntPtr.Zero) {
                return globalTable.GetTable(key);
            }

            Debug.LogError("document ptr is null");
            return null;
        }

        public ITomlTable[] GetTableArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetTableArray(key);
        }

        public bool GetBool(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            return globalTable.GetBool(key);
        }

        public float GetFloat(string key) {
            return ptr == IntPtr.Zero ? 0f : globalTable.GetFloat(key);
        }

        public float[] GetFloatArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetFloatArray(key);
        }

        public int GetInt(string key) {
            return ptr == IntPtr.Zero ? 0 : globalTable.GetInt(key);
        }

        public int[] GetIntArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetIntArray(key);
        }

        public uint GetUInt(string key) {
            return ptr == IntPtr.Zero ? 0 : globalTable.GetUInt(key);
        }

        public uint[] GetUIntArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetUIntArray(key);
        }

        public long GetLong(string key) {
            return ptr == IntPtr.Zero ? 0 : globalTable.GetLong(key);
        }

        public long[] GetLongArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetLongArray(key);
        }

        public string GetString(string key) {
            return ptr == IntPtr.Zero ? string.Empty : globalTable.GetString(key);
        }

        public string[] GetStringArray(string key) {
            return ptr == IntPtr.Zero ? null : globalTable.GetStringArray(key);
        }

        public void AddIntItem(string key, int val) {
            globalTable.AddIntItem(key, val);
        }

        public void AddIntArrayItem(string key, int[] arr) {
            globalTable.AddIntArrayItem(key, arr);
        }

        public void AddLongItem(string key, long val) {
            globalTable.AddLongItem(key, val);
        }

        public void AddLongArrayItem(string key, long[] arr) {
            globalTable.AddLongArrayItem(key, arr);
        }

        public void AddFloatItem(string key, float val) {
            globalTable.AddFloatItem(key, val);
        }

        public void AddFloatArrayItem(string key, float[] arr) {
            globalTable.AddFloatArrayItem(key, arr);
        }

        public void AddStringItem(string key, string val) {
            globalTable.AddStringItem(key, val);
        }

        public void AddStringArrayItem(string key, string[] arr) {
            globalTable.AddStringArrayItem(key, arr);
        }

        public void AddBoolItem(string key, bool val) {
            globalTable.AddBoolItem(key, val);
        }

        public ITomlTable AddTable(string key) {
            return globalTable.AddTable(key);
        }

        public ITomlTable AddInlineTable(string key) {
            return globalTable.AddInlineTable(key);
        }

        public string ConvertToString() {
            if (ptr == IntPtr.Zero) {
                return default;
            }
            IntPtr tempPtr = TomlFFI.ftoml_document_to_string(ptr);
            return TomlFFI.Ptr2String(tempPtr);
        }

        public void AddInlineTableArrayValue(string key, ref InlineTomlTable table) {
            globalTable.AddInlineTableArrayValue(key, ref table);
        }

        public void SetInlineTableArrayValue(string arrayKey, uint index, ref InlineTomlTable table) {
            globalTable.SetInlineTableArrayValue(arrayKey, index, ref table);
        }

        public ITomlTable GetTableArrayValue(string arrayKey, uint index) {
            return globalTable.GetTableArrayValue(arrayKey, index);
        }

        public void AddInlineTableArray(string arrayKey, ref InlineTomlTable[] arr) {
            globalTable.AddInlineTableArray(arrayKey, ref arr);
        }

        public void RemoveItem(string key) {
            globalTable.RemoveItem(key);
        }

        public void AddIntArrayValue(string key, int value) {
            globalTable.AddIntArrayValue(key, value);
        }

        public void AddIntArrayValue(string key, uint index, int value) {
            globalTable.AddIntArrayValue(key, index, value);
        }

        public void SetIntArrayValue(string key, uint index, int value) {
            globalTable.SetIntArrayValue(key, index, value);
        }

        public void AddLongArrayValue(string key, long value) {
            globalTable.AddLongArrayValue(key, value);
        }

        public void AddLongArrayValue(string key, uint index, long value) {
            globalTable.AddLongArrayValue(key, index, value);
        }

        public void SetLongArrayValue(string key, uint index, long value) {
            globalTable.SetLongArrayValue(key, index, value);
        }

        public void AddFloatArrayValue(string key, float value) {
            globalTable.AddFloatArrayValue(key, value);
        }

        public void AddFloatArrayValue(string key, uint index, float value) {
            globalTable.AddFloatArrayValue(key, index, value);
        }

        public void SetFloatArrayValue(string key, uint index, float value) {
            globalTable.SetFloatArrayValue(key, index, value);
        }

        public void AddStringArrayValue(string key, string value) {
            globalTable.AddStringArrayValue(key, value);
        }

        public void AddStringArrayValue(string key, uint index, string value) {
            globalTable.AddStringArrayValue(key, index, value);
        }

        public void SetStringArrayValue(string key, uint index, string value) {
            globalTable.SetStringArrayValue(key, index, value);
        }

        public void RemoveArrayValue(string key, uint index) {
            globalTable.RemoveArrayValue(key, index);
        }

        public bool AddNormalTableArrayValue(string key, ref NormalTomlTable table) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            var itemPtr = TomlFFI.ftoml_document_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return false;
            }

            var tableArrayPtr = TomlFFI.ftoml_item_as_array_of_tables(itemPtr);

            if (tableArrayPtr == IntPtr.Zero) {
                return false;
            }

            TomlFFI.ftoml_table_array_push(tableArrayPtr, table.ptr);

            var len = TomlFFI.ftoml_table_array_len(tableArrayPtr);

            if (len <= 0) {
                Debug.LogError($"table array: {key} addition failed!");
                return false;
            }
            var tablePtr = TomlFFI.ftoml_table_array_get(tableArrayPtr, len - 1);

            if (tablePtr == IntPtr.Zero) {
                Debug.LogError($"table array: {key} addition failed!");
                return false;
            }

            table.ptr = tablePtr;
            return true;
        }

        public void AddNormalTableArray(string arrayKey, ref NormalTomlTable[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr tableArrayPtr = TomlFFI.ftoml_table_array_new();
            IntPtr itemPtr = TomlFFI.ftoml_item_from_table_array(tableArrayPtr);

            TomlFFI.ftoml_document_insert(ptr, arrayKey, itemPtr);

            for (int i = 0; i < arr.Length; i++) {
                AddNormalTableArrayValue(arrayKey, ref arr[i]);
            }

            TomlFFI.ftoml_item_dispose(itemPtr);
            TomlFFI.ftoml_table_array_dispose(tableArrayPtr);
        }

        public float[][] Get2DFloatArray(string key) {
            if (ptr == IntPtr.Zero) {
                return default;
            }

            return globalTable.Get2DFloatArray(key);
        }
    }

    public struct NormalTomlTable : ITomlTable {
        internal IntPtr ptr;

        public NormalTomlTable(IntPtr ptr) {
            this.ptr = ptr;
        }

        public void Dispose() {
            ptr = IntPtr.Zero;
        }

        public bool Has(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            return TomlFFI.ftoml_table_contains_key(ptr, key);
        }

        public string[] GetKeys() {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var keysPtr = TomlFFI.ftoml_table_get_keys(ptr);

            if (keysPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_strs_len(keysPtr);

            if (length == 0) {
                TomlFFI.ftoml_strs_dispose(keysPtr);
                return null;
            }

            var keys = new string[length];

            for (var i = 0; i < length; i++) {
                var strPtr = TomlFFI.ftoml_strs_get(keysPtr, (uint)i);
                keys[i] = TomlFFI.Ptr2String(strPtr);
            }

            TomlFFI.ftoml_strs_dispose(keysPtr);
            return keys;
        }

        public TomlFieldType GetType(string key) {
            if (ptr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            if (TomlFFI.ftoml_item_is_bool(itemPtr)) {
                return TomlFieldType.Bool;
            }

            if (TomlFFI.ftoml_item_is_float(itemPtr)) {
                return TomlFieldType.Float;
            }

            if (TomlFFI.ftoml_item_is_str(itemPtr)) {
                return TomlFieldType.String;
            }

            if (TomlFFI.ftoml_item_is_integer(itemPtr)) {
                return TomlFieldType.Integer;
            }

            if (TomlFFI.ftoml_item_is_array(itemPtr)) {
                return TomlFieldType.Array;
            }

            if (TomlFFI.ftoml_item_is_table(itemPtr)) {
                return TomlFieldType.Table;
            }

            if (TomlFFI.ftoml_item_is_array_of_tables(itemPtr)) {
                return TomlFieldType.ArrayOfTable;
            }

            if (TomlFFI.ftoml_item_is_inline_table(itemPtr)) {
                return TomlFieldType.InlineTable;
            }

            return TomlFieldType.None;
        }

        public ITomlTable GetTable(string key) {
            if (ptr == IntPtr.Zero) {
                Debug.LogError("ptr is null");
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                Debug.LogError($"key {key} is not existing");
                return null;
            }

            if (TomlFFI.ftoml_item_is_table(itemPtr)) {
                var tablePtr = TomlFFI.ftoml_item_as_table(itemPtr);
                return new NormalTomlTable(tablePtr);
            }

            if (TomlFFI.ftoml_item_is_inline_table(itemPtr)) {
                var tablePtr = TomlFFI.ftoml_item_as_inline_table(itemPtr);
                return new InlineTomlTable(tablePtr);
            }

            Debug.LogError($"key {key} is not a table key");
            return null;
        }

        public ITomlTable[] GetTableArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                Debug.LogError($"key {key} is not existing");
                return null;
            }

            if (TomlFFI.ftoml_item_is_array(itemPtr)) {
                var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

                if (arrayPtr == IntPtr.Zero) {
                    return null;
                }

                var length = TomlFFI.ftoml_array_len(arrayPtr);
                var tables = new ITomlTable[length];

                for (uint i = 0; i < length; i++) {
                    var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                    if (valuePtr == IntPtr.Zero) {
                        Debug.LogError($"key {key} is not existing");
                        return null;
                    }
                    else {
                        tables[i] = new InlineTomlTable(TomlFFI.ftoml_value_as_inline_table(valuePtr));
                    }
                }

                return tables;
            }

            if (TomlFFI.ftoml_item_is_array_of_tables(itemPtr)) {
                var arrayPtr = TomlFFI.ftoml_item_as_array_of_tables(itemPtr);

                if (arrayPtr == IntPtr.Zero) {
                    return null;
                }

                var length = TomlFFI.ftoml_table_array_len(arrayPtr);
                var tables = new ITomlTable[length];

                for (uint i = 0; i < length; i++) {
                    var tablePtr = TomlFFI.ftoml_table_array_get(arrayPtr, i);

                    if (tablePtr == IntPtr.Zero) {
                        Debug.LogError($"key {key} is not existing");
                        return null;
                    }
                    else {
                        tables[i] = new NormalTomlTable(tablePtr);
                    }
                }

                return tables;
            }

            return null;
        }

        public TomlFieldType GetArrayValueType(string key) {
            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (length == 0) {
                return TomlFieldType.None;
            }

            TomlFieldType type = TomlFieldType.None;

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr != IntPtr.Zero) {
                    type = GetValueType(subValuePtr);
                }
            }

            return type;
        }

        public TomlFieldType GetValueType(IntPtr itemPtr) {
            if (itemPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            if (TomlFFI.ftoml_value_is_bool(itemPtr)) {
                return TomlFieldType.Bool;
            }

            if (TomlFFI.ftoml_value_is_float(itemPtr)) {
                return TomlFieldType.Float;
            }

            if (TomlFFI.ftoml_value_is_str(itemPtr)) {
                return TomlFieldType.String;
            }

            if (TomlFFI.ftoml_value_is_integer(itemPtr)) {
                return TomlFieldType.Integer;
            }

            if (TomlFFI.ftoml_value_is_array(itemPtr)) {
                return TomlFieldType.Array;
            }

            if (TomlFFI.ftoml_value_is_inline_table(itemPtr)) {
                return TomlFieldType.InlineTable;
            }

            return TomlFieldType.None;
        }

        public bool GetBool(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return false;
            }

            var value = TomlFFI.ftoml_item_as_bool(itemPtr);

            return value;
        }

        public float GetFloat(string key) {
            if (ptr == IntPtr.Zero) {
                return 0f;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return 0f;
            }

            var value = TomlFFI.ftoml_item_as_float(itemPtr);

            if (TomlFFI.ftoml_item_is_integer(itemPtr)) {
                value = TomlFFI.ftoml_item_as_int32(itemPtr);
            }
            return value;
        }

        public float[] GetFloatArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new float[length];

            for (uint i = 0; i < length; i++) {
                var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (valuePtr == IntPtr.Zero) {
                    array[i] = 0f;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_float(valuePtr);

                    if (TomlFFI.ftoml_value_is_integer(valuePtr)) {
                        array[i] = TomlFFI.ftoml_value_as_int32(valuePtr);
                    }
                }
            }

            return array;
        }

        public int GetInt(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_item_as_int32(itemPtr);
            return value;
        }

        public int[] GetIntArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new int[length];

            for (uint i = 0; i < length; i++) {
                var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (valuePtr == IntPtr.Zero) {
                    array[i] = 0;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_int32(valuePtr);
                }
            }

            return array;
        }

        public uint GetUInt(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_item_as_int32(itemPtr);
            return (uint)value;
        }

        public uint[] GetUIntArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new uint[length];

            for (uint i = 0; i < length; i++) {
                var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (valuePtr == IntPtr.Zero) {
                    array[i] = 0u;
                }
                else {
                    array[i] = (uint)TomlFFI.ftoml_value_as_int32(valuePtr);
                }
            }

            return array;
        }

        public long GetLong(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_item_as_int64(itemPtr);

            return value;
        }

        public long[] GetLongArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new long[length];

            for (uint i = 0; i < length; i++) {
                var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (valuePtr == IntPtr.Zero) {
                    array[i] = 0L;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_int64(valuePtr);
                }
            }

            return array;
        }

        public string GetString(string key) {
            if (ptr == IntPtr.Zero) {
                return string.Empty;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return string.Empty;
            }

            var strPtr = TomlFFI.ftoml_item_as_str(itemPtr);
            var value = TomlFFI.Ptr2String(strPtr);
            return value;
        }

        public string[] GetStringArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new string[length];

            for (uint i = 0; i < length; i++) {
                var strPtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (strPtr == IntPtr.Zero) {
                    array[i] = string.Empty;
                }
                else {
                    var valuePtr = TomlFFI.ftoml_value_as_str(strPtr);
                    array[i] = TomlFFI.Ptr2String(valuePtr);
                }
            }

            return array;
        }

        public float[][] Get2DFloatArray(string key) {
            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return default;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return default;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            float[][] arrays = new float[length][];

            for (uint j = 0; j < length; j++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, j);
                var subArrayPtr = TomlFFI.ftoml_value_as_array(subValuePtr);

                if (subArrayPtr == IntPtr.Zero) {
                    return default;
                }

                var subLength = TomlFFI.ftoml_array_len(subArrayPtr);
                arrays[j] = new float[subLength];
                for (uint i = 0; i < subLength; i++) {
                    var valuePtr = TomlFFI.ftoml_array_get(subArrayPtr, i);

                    if (valuePtr == IntPtr.Zero) {
                        arrays[j][i] = 0f;
                    }
                    else {
                        arrays[j][i] = TomlFFI.ftoml_value_as_float(valuePtr);

                        if (TomlFFI.ftoml_value_is_integer(valuePtr)) {
                            arrays[j][i] = TomlFFI.ftoml_value_as_int32(valuePtr);
                        }
                    }
                }
            }

            return arrays;
        }

        public void AddIntItem(string key, int val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_i32(val);
            if (itemPtr == IntPtr.Zero) {
                TomlFFI.ftoml_item_dispose(itemPtr);
                return;
            }
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddIntArrayItem(string key, int[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_i32(arr[i]);
                if (valPtr == IntPtr.Zero) {
                    TomlFFI.ftoml_value_dispose(valPtr);
                    return;
                }

                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_array(arrayPtr);
            if (itemPtr == IntPtr.Zero) {
                TomlFFI.ftoml_item_dispose(itemPtr);
                return;
            }
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void RemoveArrayValue(string key, uint index) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            if (TomlFFI.ftoml_item_is_array(itemPtr)) {
                var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

                var length = TomlFFI.ftoml_array_len(arrayPtr);

                if (index >= length) {
                    return;
                }

                TomlFFI.ftoml_array_remove(arrayPtr, index);
            }
            else if (TomlFFI.ftoml_item_is_array_of_tables(itemPtr)) {
                var tableArrayPtr = TomlFFI.ftoml_item_as_array_of_tables(itemPtr);

                var len = TomlFFI.ftoml_table_array_len(tableArrayPtr);

                if (index >= len) {
                    return;
                }

                TomlFFI.ftoml_table_array_remove(tableArrayPtr, index);
            }
        }

        public void AddLongItem(string key, long val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_i64(val);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddLongArrayItem(string key, long[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_i64(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_array(arrayPtr);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddLongArrayValue(string key, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }
            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddLongArrayValue(string key, uint index, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetLongArrayValue(string key, uint index, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddFloatItem(string key, float val) {
            if (ptr == IntPtr.Zero) {
                return;
            }
            IntPtr itemPtr = TomlFFI.ftoml_item_from_float(val);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddFloatArrayItem(string key, float[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_float(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_array(arrayPtr);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddFloatArrayValue(string key, uint index, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddFloatArrayValue(string key, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetFloatArrayValue(string key, uint index, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddIntArrayValue(string key, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddIntArrayValue(string key, uint index, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetIntArrayValue(string key, uint index, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddStringItem(string key, string val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_str(val);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddStringArrayItem(string key, string[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_str(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_array(arrayPtr);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public void AddStringArrayValue(string key, uint index, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddStringArrayValue(string key, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetStringArrayValue(string key, uint index, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddBoolItem(string key, bool val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_item_from_bool(val);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
        }

        public ITomlTable AddTable(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            IntPtr tablePtr = TomlFFI.ftoml_table_new();
            IntPtr itemPtr = TomlFFI.ftoml_item_from_table(tablePtr);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_table_dispose(tablePtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
            return GetTable(key);
        }

        public ITomlTable AddInlineTable(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            IntPtr tablePtr = TomlFFI.ftoml_inline_table_new();
            IntPtr itemPtr = TomlFFI.ftoml_item_from_inline_table(tablePtr);
            TomlFFI.ftoml_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_item_dispose(itemPtr);
            TomlFFI.ftoml_inline_table_dispose(tablePtr);
            return GetTable(key);
        }

        public void AddInlineTableArray(string arrayKey, ref InlineTomlTable[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            IntPtr itemPtr = TomlFFI.ftoml_item_from_array(arrayPtr);
            TomlFFI.ftoml_table_insert(ptr, arrayKey, itemPtr);

            for (int i = 0; i < arr.Length; i++) {
                AddInlineTableArrayValue(arrayKey, ref arr[i]);
            }

            TomlFFI.ftoml_item_dispose(itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
        }

        public void AddInlineTableArrayValue(string arrayKey, ref InlineTomlTable table) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, arrayKey);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero || table.ptr == IntPtr.Zero) {
                return;
            }

            var valuePtr = TomlFFI.ftoml_value_from_inline_table(table.ptr);
            TomlFFI.ftoml_inline_table_dispose(table.ptr);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            if (length <= 0) {
                table.ptr = IntPtr.Zero;
                return;
            }

            var tablePtr = TomlFFI.ftoml_array_get(arrayPtr, length - 1);

            if (tablePtr == IntPtr.Zero) {
                return;
            }

            table.ptr = TomlFFI.ftoml_value_as_inline_table(tablePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetInlineTableArrayValue(string arrayKey, uint index, ref InlineTomlTable table) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, arrayKey);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero || table.ptr == IntPtr.Zero) {
                return;
            }

            var valuePtr = TomlFFI.ftoml_value_from_inline_table(table.ptr);
            TomlFFI.ftoml_inline_table_dispose(table.ptr);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            if (length <= 0) {
                table.ptr = IntPtr.Zero;
                return;
            }

            var tablePtr = TomlFFI.ftoml_array_get(arrayPtr, index);

            if (tablePtr == IntPtr.Zero) {
                return;
            }

            table.ptr = TomlFFI.ftoml_value_as_inline_table(tablePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public ITomlTable GetTableArrayValue(string arrayKey, uint index) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, arrayKey);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            if (TomlFFI.ftoml_item_is_array(itemPtr)) {
                var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);
                if (arrayPtr == IntPtr.Zero) {
                    return null;
                }

                var length = TomlFFI.ftoml_array_len(arrayPtr);

                if (index >= length) {
                    return null;
                }
                var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, index);

                if (valuePtr == IntPtr.Zero) {
                    return null;
                }

                if (TomlFFI.ftoml_value_is_inline_table(valuePtr)) {
                    return new InlineTomlTable(TomlFFI.ftoml_value_as_inline_table(valuePtr));
                }
            }

            if (TomlFFI.ftoml_item_is_array_of_tables(itemPtr)) {
                var tableArrayPtr = TomlFFI.ftoml_item_as_array_of_tables(itemPtr);
                var length = TomlFFI.ftoml_table_array_len(tableArrayPtr);

                if (index >= length) {
                    return null;
                }
                return new NormalTomlTable(TomlFFI.ftoml_table_array_get(tableArrayPtr, index));
            }

            return null;
        }

        public ITomlTable GetNormalArrayValue(string arrayKey, uint index) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_table_get(ptr, arrayKey);

            if (itemPtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return null;
            }
            var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, index);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            if (TomlFFI.ftoml_value_is_inline_table(valuePtr)) {
                return new InlineTomlTable(TomlFFI.ftoml_value_as_inline_table(valuePtr));
            }

            var tableArrayPtr = TomlFFI.ftoml_item_as_array_of_tables(itemPtr);
            return new NormalTomlTable(TomlFFI.ftoml_table_array_get(tableArrayPtr, index));
        }


        public void RemoveItem(string key) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            TomlFFI.ftoml_table_remove(ptr, key);
        }
    }

    public struct InlineTomlTable : ITomlTable {
        internal IntPtr ptr;

        public InlineTomlTable(IntPtr ptr) {
            this.ptr = ptr;
        }

        public void Dispose() {
            ptr = IntPtr.Zero;
        }

        public bool Has(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            return TomlFFI.ftoml_inline_table_contains_key(ptr, key);
        }

        public string[] GetKeys() {
            var keysPtr = TomlFFI.ftoml_inline_table_get_keys(ptr);

            if (keysPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_strs_len(keysPtr);

            if (length == 0) {
                TomlFFI.ftoml_strs_dispose(keysPtr);
                return null;
            }

            var keys = new string[length];

            for (var i = 0; i < length; i++) {
                var strPtr = TomlFFI.ftoml_strs_get(keysPtr, (uint)i);
                keys[i] = TomlFFI.Ptr2String(strPtr);
            }

            TomlFFI.ftoml_strs_dispose(keysPtr);
            return keys;
        }

        public ITomlTable GetTable(string key) {
            if (ptr == IntPtr.Zero) {
                Debug.LogError("ptr is null");
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                Debug.LogError($"key {key} is not a inline table key");
                return null;
            }

            return new InlineTomlTable(TomlFFI.ftoml_value_as_inline_table(valuePtr));
        }

        public bool GetBool(string key) {
            if (ptr == IntPtr.Zero) {
                return false;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return false;
            }

            var value = TomlFFI.ftoml_value_as_bool(valuePtr);

            return value;
        }

        public float GetFloat(string key) {
            if (ptr == IntPtr.Zero) {
                return 0f;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return 0f;
            }

            var value = TomlFFI.ftoml_value_as_float(valuePtr);

            if (TomlFFI.ftoml_value_is_integer(valuePtr)) {
                value = TomlFFI.ftoml_value_as_int32(valuePtr);
            }

            return value;
        }

        public float[] GetFloatArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(valuePtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new float[length];

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr == IntPtr.Zero) {
                    array[i] = 0f;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_float(subValuePtr);

                    if (TomlFFI.ftoml_value_is_integer(subValuePtr)) {
                        array[i] = TomlFFI.ftoml_value_as_int32(subValuePtr);
                    }
                }
            }

            return array;
        }

        public int GetInt(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_value_as_int32(valuePtr);
            return value;
        }

        public int[] GetIntArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(valuePtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new int[length];

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr == IntPtr.Zero) {
                    array[i] = 0;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_int32(subValuePtr);
                }
            }

            return array;
        }

        public uint GetUInt(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_value_as_int32(valuePtr);
            return (uint)value;
        }

        public uint[] GetUIntArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(valuePtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new uint[length];

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr == IntPtr.Zero) {
                    array[i] = 0u;
                }
                else {
                    array[i] = (uint)TomlFFI.ftoml_value_as_int32(subValuePtr);
                }
            }
            return array;
        }

        public long GetLong(string key) {
            if (ptr == IntPtr.Zero) {
                return 0;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return 0;
            }

            var value = TomlFFI.ftoml_value_as_int64(valuePtr);

            return value;
        }

        public long[] GetLongArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(valuePtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new long[length];

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr == IntPtr.Zero) {
                    array[i] = 0L;
                }
                else {
                    array[i] = TomlFFI.ftoml_value_as_int64(subValuePtr);
                }
            }

            return array;
        }

        public string GetString(string key) {
            if (ptr == IntPtr.Zero) {
                return string.Empty;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return string.Empty;
            }

            var strPtr = TomlFFI.ftoml_value_as_str(valuePtr);
            var value = TomlFFI.Ptr2String(strPtr);
            return value;
        }

        public string[] GetStringArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var valuePtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (valuePtr == IntPtr.Zero) {
                return null;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(valuePtr);

            if (arrayPtr == IntPtr.Zero) {
                return null;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            var array = new string[length];

            for (uint i = 0; i < length; i++) {
                var strPtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (strPtr == IntPtr.Zero) {
                    array[i] = string.Empty;
                }
                else {
                    var valPtr = TomlFFI.ftoml_value_as_str(strPtr);
                    array[i] = TomlFFI.Ptr2String(valPtr);
                }
            }
            return array;
        }

        public TomlFieldType GetType(string key) {
            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);
            return GetType(itemPtr);
        }

        public TomlFieldType GetType(IntPtr itemPtr) {
            if (itemPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            if (TomlFFI.ftoml_value_is_bool(itemPtr)) {
                return TomlFieldType.Bool;
            }

            if (TomlFFI.ftoml_value_is_float(itemPtr)) {
                return TomlFieldType.Float;
            }

            if (TomlFFI.ftoml_value_is_str(itemPtr)) {
                return TomlFieldType.String;
            }

            if (TomlFFI.ftoml_value_is_integer(itemPtr)) {
                return TomlFieldType.Integer;
            }

            if (TomlFFI.ftoml_value_is_array(itemPtr)) {
                return TomlFieldType.Array;
            }

            if (TomlFFI.ftoml_value_is_inline_table(itemPtr)) {
                return TomlFieldType.InlineTable;
            }

            return TomlFieldType.None;
        }

        public TomlFieldType GetArrayValueType(string key) {
            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return TomlFieldType.None;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (length == 0) {
                return TomlFieldType.None;
            }

            TomlFieldType type = TomlFieldType.None;

            for (uint i = 0; i < length; i++) {
                var subValuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                if (subValuePtr != IntPtr.Zero) {
                    type = type == TomlFieldType.Float ? TomlFieldType.Float : GetType(subValuePtr);
                }
            }
            return type;
        }

        public ITomlTable[] GetTableArray(string key) {
            if (ptr == IntPtr.Zero) {
                return null;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                Debug.LogError($"key {key} is not existing");
                return null;
            }

            if (TomlFFI.ftoml_value_is_inline_table(itemPtr)) {
                return null;
            }

            if (TomlFFI.ftoml_item_is_array(itemPtr)) {
                var arrayPtr = TomlFFI.ftoml_item_as_array(itemPtr);

                if (arrayPtr == IntPtr.Zero) {
                    return null;
                }

                var length = TomlFFI.ftoml_array_len(arrayPtr);
                var tables = new ITomlTable[length];

                for (uint i = 0; i < length; i++) {
                    var valuePtr = TomlFFI.ftoml_array_get(arrayPtr, i);

                    if (valuePtr == IntPtr.Zero) {
                        Debug.LogError($"key {key} is not existing");
                        return null;
                    }
                    else {
                        tables[i] = new InlineTomlTable(TomlFFI.ftoml_value_as_inline_table(valuePtr));
                    }
                }

                return tables;
            }

            return null;
        }

        public float[][] Get2DFloatArray(string key) {
            return default;
        }

        public void AddIntItem(string key, int val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_i32(val);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddIntArrayItem(string key, int[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_i32(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_array(arrayPtr);

            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddLongItem(string key, long val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_i64(val);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddLongArrayItem(string key, long[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_i64(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_array(arrayPtr);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void SetLongArrayValue(string key, uint index, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddLongArrayValue(string key, uint index, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddLongArrayValue(string key, long value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_i64(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddFloatItem(string key, float val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_float(val);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddFloatArrayItem(string key, float[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_float(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_array(arrayPtr);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddStringItem(string key, string val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_str(val);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddStringArrayItem(string key, string[] arr) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr arrayPtr = TomlFFI.ftoml_array_new();
            for (int i = 0; i < arr.Length; i++) {
                IntPtr valPtr = TomlFFI.ftoml_value_from_str(arr[i]);
                TomlFFI.ftoml_array_push(arrayPtr, valPtr);
                TomlFFI.ftoml_value_dispose(valPtr);
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_array(arrayPtr);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_array_dispose(arrayPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }

        public void AddBoolItem(string key, bool val) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            IntPtr itemPtr = TomlFFI.ftoml_value_from_bool(val);
            TomlFFI.ftoml_inline_table_insert(ptr, key, itemPtr);
            TomlFFI.ftoml_value_dispose(itemPtr);
        }
        public void AddIntArrayValue(string key, uint index, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddIntArrayValue(string key, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetIntArrayValue(string key, uint index, int value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_i32(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddFloatArrayValue(string key, uint index, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddFloatArrayValue(string key, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetFloatArrayValue(string key, uint index, float value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_float(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddStringArrayValue(string key, uint index, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }
            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_insert(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void AddStringArrayValue(string key, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);
            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_push(arrayPtr, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void SetStringArrayValue(string key, uint index, string value) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }
            IntPtr valuePtr = TomlFFI.ftoml_value_from_str(value);
            TomlFFI.ftoml_array_replace(arrayPtr, index, valuePtr);
            TomlFFI.ftoml_value_dispose(valuePtr);
        }

        public void RemoveArrayValue(string key, uint index) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            var itemPtr = TomlFFI.ftoml_inline_table_get(ptr, key);

            if (itemPtr == IntPtr.Zero) {
                return;
            }

            var arrayPtr = TomlFFI.ftoml_value_as_array(itemPtr);

            if (arrayPtr == IntPtr.Zero) {
                return;
            }

            var length = TomlFFI.ftoml_array_len(arrayPtr);

            if (index >= length) {
                return;
            }

            TomlFFI.ftoml_array_remove(arrayPtr, index);
        }

        public void RemoveItem(string key) {
            if (ptr == IntPtr.Zero) {
                return;
            }

            TomlFFI.ftoml_inline_table_remove(ptr, key);
        }
    }

    public interface ITomlTable : IDisposable {
        public bool Has(string key);
        public string[] GetKeys();
        public ITomlTable GetTable(string key);
        public bool GetBool(string key);
        public float GetFloat(string key);
        public float[] GetFloatArray(string key);
        public int GetInt(string key);
        public int[] GetIntArray(string key);
        public uint GetUInt(string key);
        public uint[] GetUIntArray(string key);
        public long GetLong(string key);
        public long[] GetLongArray(string key);
        public string GetString(string key);
        public string[] GetStringArray(string key);
        public TomlFieldType GetType(string key);
        public TomlFieldType GetArrayValueType(string key);
        public void AddIntItem(string key, int val);
        public void RemoveItem(string key);
        public void AddIntArrayItem(string key, int[] arr);
        public void RemoveArrayValue(string key, uint index);
        public void AddLongItem(string key, long val);
        public void AddLongArrayItem(string key, long[] arr);
        public void AddLongArrayValue(string key, uint index, long value);
        public void AddLongArrayValue(string key, long value);
        public void SetLongArrayValue(string key, uint index, long value);
        public void AddFloatItem(string key, float val);
        public void AddFloatArrayItem(string key, float[] arr);
        public void AddFloatArrayValue(string key, uint index, float value);
        public void AddFloatArrayValue(string key, float value);
        public void SetFloatArrayValue(string key, uint index, float value);
        public void AddIntArrayValue(string key, int value);
        public void AddIntArrayValue(string key, uint index, int value);
        public void SetIntArrayValue(string key, uint index, int value);
        public void AddStringItem(string key, string val);
        public void AddStringArrayItem(string key, string[] arr);
        public void AddStringArrayValue(string key, uint index, string value);
        public void AddStringArrayValue(string key, string value);
        public void SetStringArrayValue(string key, uint index, string value);
        public void AddBoolItem(string key, bool val);
        public ITomlTable[] GetTableArray(string key);
        public float[][] Get2DFloatArray(string key);
    }
}
