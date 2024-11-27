using System.Collections.Generic;
using UnityEngine;

public enum MileSkinningCullingMode {
    AlwaysAnimate,
    CullUpdateTransforms,
    CullCompletely
}

public class MileSkinningData {
    public GPUSkinningAnimation gpuSkinningAnimation;
    public Mesh mesh;
    public Texture2D texture2D;

    public List<MileSkinning> mileSkinnings = new List<MileSkinning>();
    private MileSkinningMaterial[] mileSkinningMaterials;

    private static int shaderPropID_GPUSkinning_TextureMatrix = -1;
    private static int shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame = 0;
    private static int shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation = 0;
    private static int shaderPorpID_GPUSkinning_Extra_Property = 0;

    public MileSkinningData() {
        if (shaderPropID_GPUSkinning_TextureMatrix == -1) {
            shaderPropID_GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_TextureSize_NumPixelsPerFrame");
            shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");
            shaderPorpID_GPUSkinning_Extra_Property = Shader.PropertyToID("_GPUSkinning_Extra_Property");
        }
    }


    public void InitMaterial(Material material, HideFlags hideFlags) {
        if (mileSkinningMaterials != null) {
            return;
        }

        mileSkinningMaterials = new MileSkinningMaterial[1];
        mileSkinningMaterials[0] = new MileSkinningMaterial() { Material = new Material(material) };
        mileSkinningMaterials[0].Material.name = "GPUSkinning";
        mileSkinningMaterials[0].Material.hideFlags = hideFlags;
        mileSkinningMaterials[0].Material.enableInstancing = true;
    }

    CullingGroup cullingGroup = null;
    MileSkinningList<BoundingSphere> cullingBounds = new MileSkinningList<BoundingSphere>(100);

    public void AddCullingBounds() {
        if (cullingGroup == null) {
            cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = Camera.main;
            cullingGroup.SetDistanceReferencePoint(Camera.main.transform);
            cullingGroup.onStateChanged = OnLodCullingGroupOnStateChangedHandler;
        }
    }

    public void RemoveCullingBounds(int index) {
        cullingBounds.RemoveAt(index);
        cullingGroup.SetBoundingSpheres(cullingBounds.buffer);
        cullingGroup.SetBoundingSphereCount(mileSkinnings.Count);
    }

    void DestroyCullingGroup() {
        if (cullingGroup != null) {
            cullingGroup.Dispose();
            cullingGroup = null;
        }
    }

    private void OnLodCullingGroupOnStateChangedHandler(CullingGroupEvent evt) {
        MileSkinning mileSkinning = mileSkinnings[evt.index];
        if (evt.isVisible) {
            mileSkinning.Player.Visible = true;
        }
        else {
            mileSkinning.Player.Visible = false;
        }
    }

    public MileSkinningMaterial GetMaterial() {
        return mileSkinningMaterials[0];
    }

    private float time = 0;
    public float Time {
        get { return time; }
        set { time = value; }
    }

    MilesSkinningExecuteOncePerFrame executeOncePerFrame = new MilesSkinningExecuteOncePerFrame();
    public void Update(float deltaTime, MileSkinningMaterial material) {

        if (executeOncePerFrame.CanBeExecute()) {
            executeOncePerFrame.MarkAsExecuted();
            time += deltaTime;
            UpdateCullingBounds();
        }

        if (material.executeOncePerFrame.CanBeExecute()) {
            material.executeOncePerFrame.MarkAsExecuted();
            material.Material.SetTexture(shaderPropID_GPUSkinning_TextureMatrix, texture2D);
            material.Material.SetVector(shaderPropID_GPUSkinning_TextureSize_NumPixelsPerFrame, new Vector4(gpuSkinningAnimation.textureWidth, gpuSkinningAnimation.textureHeight, gpuSkinningAnimation.bones.Length * 3, 0));
        }
    }

    private void UpdateCullingBounds() {
        int numPlayers = mileSkinnings.Count;
        for (int i = 0; i < numPlayers; ++i) {
            BoundingSphere bounds = cullingBounds[i];
            MileSkinning skinning = mileSkinnings[i];
            bounds.position = skinning.Player.Position;
            bounds.radius = gpuSkinningAnimation.sphereRadius;
            cullingBounds[i] = bounds;
        }
    }

    public void UpdatePlayingData(
        MaterialPropertyBlock materialPropertyBlock,
        GPUSkinningClip clip,
        int frameIndex
        /*
        GPUSkinningFrame frame,
        bool rootMotionEnabled,
        GPUSkinningClip lastPlayedClip,
        int frameIndex_crossFade,
        float crossFadeTime,
        float crossFadeProgress
        */
        ) {
        materialPropertyBlock.SetVector(shaderPorpID_GPUSkinning_FrameIndex_PixelSegmentation, new Vector4(frameIndex, clip.pixelSegmentation, 0, 0));
    }

    public void UpdateExtraProperty(MaterialPropertyBlock materialPropertyBlock, Vector4 extraProp) {
        materialPropertyBlock.SetVector(shaderPorpID_GPUSkinning_Extra_Property, extraProp);
    }

    public void Destroy() {
        gpuSkinningAnimation = null;
        mesh = null;

        if (cullingBounds != null) {
            cullingBounds.Release();
            cullingBounds = null;
        }

        DestroyCullingGroup();

        if (mileSkinningMaterials != null) {
            for (int i = 0; i < mileSkinningMaterials.Length; ++i) {
                mileSkinningMaterials[i].Destroy();
                mileSkinningMaterials[i] = null;
            }
            mileSkinningMaterials = null;
        }

        if (texture2D != null) {
            Object.DestroyImmediate(texture2D);
            texture2D = null;
        }

        if (mileSkinnings != null) {
            mileSkinnings.Clear();
            mileSkinnings = null;
        }
    }
}