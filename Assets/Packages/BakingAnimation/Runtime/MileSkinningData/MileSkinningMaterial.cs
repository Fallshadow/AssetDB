using UnityEngine;

public class MileSkinningMaterial {

    Material material = null;
    public Material Material { get; set; }

    public MilesSkinningExecuteOncePerFrame executeOncePerFrame = new MilesSkinningExecuteOncePerFrame();

    public void Destroy() {
        if (material != null) {
            Object.Destroy(material);
            material = null;
        }
    }
}