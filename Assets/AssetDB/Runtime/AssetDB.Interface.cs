using FallShadow.Common;

namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {
        public bool IsValid(Handle<UAsset> handle) {
            return handleManager.IsValid(handle);
        }

        public bool IsLoading(Handle<UAsset> handle) {
            return GetStatus(handle) == Status.Loading;
        }

        public bool IsSucceeded(Handle<UAsset> handle) {
            return GetStatus(handle) == Status.Succeeded;
        }

        public bool IsFailed(Handle<UAsset> handle) {
            return GetStatus(handle) == Status.Failed;
        }
    }
}