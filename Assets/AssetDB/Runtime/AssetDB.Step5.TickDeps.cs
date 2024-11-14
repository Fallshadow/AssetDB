using FallShadow.Common;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    // 对于每一个 bundle 都创建 bundleKey2Deps 不管有没有依赖
    // 有依赖，tick 之后会添加内容，没有就不添加
    public partial class AssetDB {
        private int depsTaskCount;
        private DepsTask[] depsTasks;

        // 一个 bundle 对应的依赖资源项
        private Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Deps;

        private void InitDeps() {
            depsTaskCount = 0;
            depsTasks = new DepsTask[MaxBundleTaskCount];
            bundleKey2Deps = new Dictionary<FixedString512Bytes, NativeList<FixedString512Bytes>>();
        }

        private void DisposeDeps() {
            depsTasks = null;

            if (bundleKey2Deps != null) {
                foreach (var dependencies in bundleKey2Deps.Values) {
                    if (dependencies.IsCreated) {
                        dependencies.Dispose();
                    }
                }

                bundleKey2Deps.Clear();
                bundleKey2Deps = null;
            }
        }

        private void TickDepsTasks() {
            for (var t = 0; t < depsTaskCount; t++) {
                ref var task = ref depsTasks[t];

                if (bundleKey2Deps.ContainsKey(task.bundleKey)) {
                    requestDepsTaskConsumeAt(ref t);
                    continue;
                }

                if (task.webOperation == null) {
                    var filePath = GetDepsFilePath(task.bundleKey);

                    if (string.IsNullOrEmpty(filePath)) {
                        bundleKey2Deps[task.bundleKey] = new NativeList<FixedString512Bytes>(0, Allocator.Persistent);
                        requestDepsTaskConsumeAt(ref t);
                        continue;
                    }

                    var request = UnityWebRequest.Get(filePath);
                    task.webOperation = request.SendWebRequest();
                }

                // TODO：下载下来的内容似乎是一列列的依赖资源路径，具体是啥需要测试
                if (task.webOperation.isDone && task.webOperation.webRequest.isDone) {
                    if (string.IsNullOrEmpty(task.webOperation.webRequest.error)) {
                        var bytes = task.webOperation.webRequest.downloadHandler.data;
                        AddDeps(task.bundleKey, bytes);
                    }

                    task.webOperation = null;
                    requestDepsTaskConsumeAt(ref t);
                }
            }
        }

        private void AddDeps(FixedString512Bytes bundleKey, byte[] bytes) {
            if (bytes == null || bytes.Length == 0) {
                return;
            }

            unsafe {
                var ptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0).ToPointer();
                var cursor = 0;
                var length = bytes.Length;
                NativeList<FixedString512Bytes> deps;

                if (bundleKey2Deps.ContainsKey(bundleKey)) {
                    deps = bundleKey2Deps[bundleKey];
                }
                else {
                    deps = new NativeList<FixedString512Bytes>(16, Allocator.Persistent);
                    bundleKey2Deps[bundleKey] = deps;
                }

                for (var i = 0; i < length; i++) {
                    var item = ptr[i];

                    if (item == '\n') {
                        var dep = FixedStringUtil.GetFixedString512(ptr, cursor, i - cursor);

                        if (dep[dep.Length - 1] == '\r') {
                            dep.Length -= 1;
                        }

                        deps.Add(dep);
                        cursor = i + 1;
                    }
                }
            }
        }
    }
}