using FallShadow.Common;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        private NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes> subSceneFileName2Path;
        private NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes> url2ContentCatalogPath;

        private void InitEcsContainer() {
            subSceneFileName2Path = new NativeParallelHashMap<FixedString128Bytes, FixedString512Bytes>(maxSubSceneFileCount, Allocator.Persistent);
            url2ContentCatalogPath = new NativeParallelHashMap<FixedString512Bytes, FixedString512Bytes>(maxSubSceneCatalogCount, Allocator.Persistent);
        }

        private void ReleaseEcsContainer() {
            if (subSceneFileName2Path.IsCreated) {
                subSceneFileName2Path.Dispose();
            }

            if (url2ContentCatalogPath.IsCreated) {
                url2ContentCatalogPath.Dispose();
            }
        }
    }
}