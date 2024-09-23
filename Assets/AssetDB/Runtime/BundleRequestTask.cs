using Unity.Collections;
using UnityEngine.Networking;

namespace FallShadow.Asset.Runtime {
    public struct BundleRequestTask {
        public FixedString512Bytes bundleKey;
        public UnityWebRequestAsyncOperation webOperation;
    }
}