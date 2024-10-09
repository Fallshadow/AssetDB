using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageAssetDB : MonoBehaviour {

    AssetDBAtlasSpriteLoader.AtlasSpriteHandle handle;

    Image image;
    public Image Image {
        get {
            if (image == null) {
                image = this.gameObject.GetComponent<Image>();
            }
            return image;
        }
    }

    string spriteUrl = "";

    public void Update() {
        if (handle.isValid) {
            if (handle.isDone) {
                var sprite = handle.sprite;
                if (sprite != null) {
                    Image.overrideSprite = sprite;
                    Image.enabled = true;
                    handle = AssetDBAtlasSpriteLoader.AtlasSpriteHandle.invalid;
                }
                else {
                    Debug.LogError("图标路径加载失败：" + spriteUrl);
                    handle = AssetDBAtlasSpriteLoader.AtlasSpriteHandle.invalid;
                    SetSprite(UISpritePathTool.GetCommonIcon("icon_load_null"));
                }
            }
        }
    }

    public void SetSprite(string url) {
        spriteUrl = url;
        if (Image.sprite == null) {
            Image.enabled = false;
        }
        
        int lastIndex = url.LastIndexOf('/');

        string imageName = url.Substring(lastIndex + 1);

        string atlasPath = url.Substring(0, lastIndex);
        lastIndex = atlasPath.LastIndexOf("/");
        string atlasName = atlasPath.Substring(lastIndex + 1);

        atlasPath = $"{atlasPath}/{atlasName}.spriteatlas";

        handle = ResHelper.dbAtlasSpriteLoader.LoadSprite(atlasPath, imageName);
    }
}