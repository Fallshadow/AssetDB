using FallShadow.Common;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        // 资源简要路径和资源详细属性的映射
        // example 1:
        // Key: "asset://graphics/pipelines.bundle"
        // value: AssetInfo.type = TaskType.Bundle AssetInfo.bundleKey = "graphics/pipelines.bundle" 非具体资源所以没有 assetPath
        // example 2:
        // Key: "asset://graphics/pipelines/forwardrenderer.asset"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "graphics/pipelines.bundle"  AssetInfo.assetPath = "forwardrenderer.asset"
        // example 3:
        // Key: "asset://samples/tps/arts/character/clips/chr_player_actor/clr_fall2idle.anim"
        // value: AssetInfo.type = TaskType.BundleAsset AssetInfo.bundleKey = "samples/tps/arts/character/clips.bundle"  AssetInfo.assetPath = "chr_player_actor/clr_fall2idle.anim"
        internal NativeHashMap<FixedString512Bytes, AssetInfo> url2AssetInfo;
        // 一个 bundle 对应多个资源 Asset
        // 这里的 bundle 是类似 "graphics/pipelines.bundle" 这种不带前缀也不带 hash 的，里面的资源也都是相对于 bundle 的。
        private NativeParallelHashMap<FixedString512Bytes, NativeList<FixedString512Bytes>> bundleKey2Assets;
    }
}