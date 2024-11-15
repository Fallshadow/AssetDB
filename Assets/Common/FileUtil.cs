using Unity.Collections;

namespace FallShadow.Common {
    public static class FileUtil {
        /// <summary>
        /// 返回值代表是否符合 32位 hash 规则，此接口需要先判断，符合规则再使用 out 值
        /// </summary>
        /// <param name="filePath"> 文件路径 </param>
        /// <param name="fileNoHashName"> 不带 hash 的名称结果值 </param>
        /// <returns></returns>
        public static bool FilePathExclude32Hash(FixedString512Bytes filePath, out FixedString512Bytes fileNoHashName) {
            bool isInRule = false;
            fileNoHashName = default;

            var sepIndex = filePath.LastIndexOf(SymbolStringUtil.Sep);
            // fileWithExt: prefabs_a515803f18202a07244ee82b16b6434d.bundle
            var fileWithExt = sepIndex == -1 ? filePath : FixedStringUtil.Substring(filePath, sepIndex + 1);
            // BuildAssetBundleOptions.AppendHashToAssetBundleName 生成的 bundle 相关文件名会额外带上 "_" + 32 位的 hash
            var underLineIndex = fileWithExt.LastIndexOf(SymbolStringUtil.UnderLineSep);
            var extIndex = fileWithExt.LastIndexOf(SymbolStringUtil.ExtSep);

            if (extIndex != -1) {
                // ext: 比如 .bundle
                var ext = FixedStringUtil.Substring(fileWithExt, extIndex);

                if (underLineIndex != -1) {
                    // "_" 和 "." 插值必须是 33
                    if (underLineIndex + 33 == extIndex) {
                        // fileNoHashName: prefabs.bundle
                        fileNoHashName = $"{FixedStringUtil.Substring(fileWithExt, 0, underLineIndex)}{ext}";
                        isInRule = true;

                        if (sepIndex != -1) {
                            var dic = FixedStringUtil.Substring(filePath, 0, sepIndex);
                            fileNoHashName = $"{dic}{SymbolStringUtil.Sep}{fileNoHashName}";
                        }
                    }
                }
            }

            return isInRule;
        }
    }
}
