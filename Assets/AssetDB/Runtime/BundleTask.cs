using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using FallShadow.Common;

namespace FallShadow.Asset.Runtime {
    public struct BundleTask {
        public Handle<UAsset> handle;
        public FixedString512Bytes bundleKey;
        public bool markAsUnload;
        public AssetBundleCreateRequest createRequest;
        public bool isRemoteAsset;
        public UnityWebRequestAsyncOperation webRequestAsyncOperation;
    }
}
