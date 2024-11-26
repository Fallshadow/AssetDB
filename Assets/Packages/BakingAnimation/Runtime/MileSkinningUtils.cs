using UnityEngine;

public class MileSkinningUtils {
    public static Texture2D CreateTexture2D(TextAsset textAsset, GPUSkinningAnimation animation) {
        if (textAsset == null || animation == null) {
            return null;
        }

        Texture2D texture2D = new Texture2D(animation.textureWidth, animation.textureHeight, TextureFormat.RGBAHalf, false, true);
        texture2D.name = "GPUSkinningTextureMatrix";
        texture2D.filterMode = FilterMode.Point;
        texture2D.LoadRawTextureData(textAsset.bytes);
        texture2D.Apply(false, true);

        return texture2D;
    }
}