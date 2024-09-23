using FallShadow.Common;
using Unity.Collections;
using UnityEngine;

namespace FallShadow.Asset.Runtime {
    public struct AssetCache {
        public Handle<UAsset> handle;
        public FixedString512Bytes url;
        public bool succeed;
        public Object asset;
        public string text;
        public byte[] bytes;
        public int frame;
    }
}