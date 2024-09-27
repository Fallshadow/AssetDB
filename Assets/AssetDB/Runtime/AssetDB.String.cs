using FallShadow.Asset.Runtime;
using FallShadow.Common;
using Unity.Collections;


namespace FallShadow.Asset.Runtime {
    public partial class AssetDB {

        private const string httpsSep = "https://";
        private const string httpSep = "http://";
        private static readonly FixedString32Bytes sep = SymbolStringUtil.Sep;
        private static readonly FixedString32Bytes underLineSep = SymbolStringUtil.UnderLineSep;
        private static readonly FixedString32Bytes extSep = SymbolStringUtil.ExtSep;
        private static readonly FixedString32Bytes protocolSep = "asset://";
        private static readonly FixedString32Bytes bundleSep = ".bundle";
        private static readonly FixedString32Bytes sceneSep = ".unity";
        private static readonly FixedString32Bytes loadAllAssetsSep = "/*";
        private static readonly FixedString32Bytes depsSep = ".deps";

        // ECS œ‡πÿ
        private static readonly FixedString32Bytes entitySceneDir = "entityscenes";
        private static readonly FixedString32Bytes contentArchiveDir = "content_archives";
        private static readonly FixedString64Bytes contentFileName = "/content_archives/archive_dependencies.bin";

        
    }
}
