using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MileSkinning : MonoBehaviour {
    static MileSkinningManager mileSkinningManager = new MileSkinningManager();

    [SerializeField] GPUSkinningAnimation gpuSkinningAnimation;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
    [SerializeField] TextAsset textAsset;
    [SerializeField] int defaultPlayingClipIndex = 0;
    [SerializeField] MileSkinningCullingMode mileSkinningCullingMode = MileSkinningCullingMode.CullUpdateTransforms;

    MileSkinningPlayer player;
    public MileSkinningPlayer Player {
        get {
            return player;
        }
    }

    void Start() {
        Init();
#if UNITY_EDITOR
        Update_Editor(0);
#endif
    }

    public void Init() {
        if (player != null) {
            return;
        }

        if (gpuSkinningAnimation != null && material != null && mesh != null && textAsset != null) {
            MileSkinningData data = null;

            if (Application.isPlaying) {
                mileSkinningManager.Register(gpuSkinningAnimation, mesh, material, textAsset, this, out data);
            }
            else {
                data = new MileSkinningData();
                data.gpuSkinningAnimation = gpuSkinningAnimation;
                data.mesh = mesh;
                data.InitMaterial(material, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
                data.texture2D = MileSkinningUtils.CreateTexture2D(textAsset, gpuSkinningAnimation);
                data.texture2D.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }
            player = new MileSkinningPlayer(gameObject, data);
            player.MileSkinningCullingMode = mileSkinningCullingMode;

            if (gpuSkinningAnimation != null && gpuSkinningAnimation.clips != null && gpuSkinningAnimation.clips.Length > 0) {
                player.Play(gpuSkinningAnimation.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, gpuSkinningAnimation.clips.Length)].name);
            }
        }
    }


#if UNITY_EDITOR
    public void Update_Editor(float deltaTime) {
        if (player != null && !Application.isPlaying) {
            player.Update_Editor(deltaTime);
        }
    }
#endif
}

