using System.Collections.Generic;
using UnityEngine;

public class MileSkinningManager {
    private List<MileSkinningData> items = new List<MileSkinningData>();

    public void Register(GPUSkinningAnimation animation, Mesh mesh, Material material, TextAsset textAsset, MileSkinning mileSkinning, out MileSkinningData mileSkinningData) {
        mileSkinningData = null;
        if (animation == null || mesh == null || material == null || textAsset == null) {
            return;
        }

        int num = items.Count;
        for (int i = 0; i < num; ++i) {
            if (items[i].gpuSkinningAnimation.guid == animation.guid) {
                mileSkinningData = items[i];
                break;
            }
        }

        if (mileSkinningData == null) {
            mileSkinningData = new MileSkinningData();
            items.Add(mileSkinningData);
        }

        if (mileSkinningData.gpuSkinningAnimation == null) {
            mileSkinningData.gpuSkinningAnimation = animation;
        }

        if (mileSkinningData.mesh == null) {
            mileSkinningData.mesh = mesh;
        }

        mileSkinningData.InitMaterial(material, HideFlags.None);

        if (mileSkinningData.texture2D == null) {
            mileSkinningData.texture2D = MileSkinningUtils.CreateTexture2D(textAsset, animation);
        }

        if (!mileSkinningData.mileSkinnings.Contains(mileSkinning)) {
            mileSkinningData.mileSkinnings.Add(mileSkinning);
            mileSkinningData.AddCullingBounds();
        }
    }

    public void Unregister(MileSkinning mileSkinning) {
        if (mileSkinning == null) {
            return;
        }

        int numItems = items.Count;
        for (int i = 0; i < numItems; ++i) {
            int playerIndex = items[i].mileSkinnings.IndexOf(mileSkinning);
            if (playerIndex != -1) {
                items[i].mileSkinnings.RemoveAt(playerIndex);
                items[i].RemoveCullingBounds(playerIndex);
                if (items[i].mileSkinnings.Count == 0) {
                    items[i].Destroy();
                    items.RemoveAt(i);
                }
                break;
            }
        }
    }
}