using FallShadow.Common;
using Unity.Collections;
using UnityEngine.Networking;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    public struct AssetTask {
        public Handle<UAsset> handle;
        public FixedString512Bytes url;
        public bool isDone;
        public AssetBundleRequest request;
        public UnityWebRequestAsyncOperation webOperation;
        public int delayFrame;
        public float delaySeconds;
    }
}