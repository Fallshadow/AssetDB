using Unity.Collections;

namespace FallShadow.Common {
    public static class FileUtil {
        /// <summary>
        /// ����ֵ�����Ƿ���� 32λ hash ���򣬴˽ӿ���Ҫ���жϣ����Ϲ�����ʹ�� out ֵ
        /// </summary>
        /// <param name="filePath"> �ļ�·�� </param>
        /// <param name="fileNoHashName"> ���� hash �����ƽ��ֵ </param>
        /// <returns></returns>
        public static bool FilePathExclude32Hash(FixedString512Bytes filePath, out FixedString512Bytes fileNoHashName) {
            bool isInRule = false;
            fileNoHashName = default;

            var sepIndex = filePath.LastIndexOf(SymbolStringUtil.Sep);
            // fileWithExt: prefabs_a515803f18202a07244ee82b16b6434d.bundle
            var fileWithExt = sepIndex == -1 ? filePath : FixedStringUtil.Substring(filePath, sepIndex + 1);
            // BuildAssetBundleOptions.AppendHashToAssetBundleName ���ɵ� bundle ����ļ����������� "_" + 32 λ�� hash
            var underLineIndex = fileWithExt.LastIndexOf(SymbolStringUtil.UnderLineSep);
            var extIndex = fileWithExt.LastIndexOf(SymbolStringUtil.ExtSep);

            if (extIndex != -1) {
                // ext: ���� .bundle
                var ext = FixedStringUtil.Substring(fileWithExt, extIndex);

                if (underLineIndex != -1) {
                    // "_" �� "." ��ֵ������ 33
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
