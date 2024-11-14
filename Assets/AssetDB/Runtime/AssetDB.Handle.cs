using FallShadow.Common;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    public struct UAsset { }
    public partial class AssetDB {

        internal HandleManager<UAsset> handleManager;
        // 协议 url 和资源信息的映射，已经加载过的资源就在这里，可以从这里进行加载
        // url 格式：asset://foo/bar.prefab
        internal NativeHashMap<FixedString512Bytes, Handle<UAsset>> url2handle;
        // index: handle.index, value: refCount 对应资源的引用计数，用于释放。
        internal NativeArray<int> refCounts;
        // example:
        // key: new Handle<UAsset>(1, 1).index
        // Value: AssetBundle
        internal Dictionary<int, AssetBundle> index2Bundle;

        private const int maxAssetCount = 4096;

        private void InitHandle() {
            handleManager = new HandleManager<UAsset>(maxAssetCount);
            url2handle = new NativeHashMap<FixedString512Bytes, Handle<UAsset>>(maxAssetCount, Allocator.Persistent);
            refCounts = new NativeArray<int>(maxAssetCount, Allocator.Persistent);
            index2Bundle = new Dictionary<int, AssetBundle>();
        }

        private void DisposeHandle() {
            if (handleManager.IsCreated()) {
                handleManager.Dispose();
            }

            if (url2handle.IsCreated) {
                url2handle.Dispose();
            }

            if (refCounts.IsCreated) {
                refCounts.Dispose();
            }

            if (index2Bundle != null) {
                foreach (var bundle in index2Bundle.Values) {
                    if (bundle != null) {
                        bundle.Unload(true);
                    }
                }
            }

            index2Bundle = null;
        }
    }
}