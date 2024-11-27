using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MileSkinning : MonoBehaviour {

    private static MileSkinningManager mileSkinningManager = new MileSkinningManager();

    [SerializeField] private GPUSkinningAnimation gpuSkinningAnimation;
    [SerializeField] private Material material;
    [SerializeField] private Mesh mesh;
    [SerializeField] private TextAsset textAsset;
    [SerializeField] private int defaultPlayingClipIndex = 0;
    [SerializeField] private MileSkinningCullingMode mileSkinningCullingMode = MileSkinningCullingMode.CullUpdateTransforms;

    private MileSkinningPlayer player;
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

    private void Update() {
        if (player != null) {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                player.Update(Time.deltaTime);
            }
            else {
                player.Update_Editor(0);
            }
#else
            player.Update(Time.deltaTime);
#endif
        }
    }

    private void OnDestroy() {
        player = null;
        gpuSkinningAnimation = null;
        mesh = null;
        material = null;
        textAsset = null;

        if (Application.isPlaying) {
            mileSkinningManager.Unregister(this);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) {
            Resources.UnloadUnusedAssets();
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
        }
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

    public void DeletePlayer() {
        player = null;
    }

    public void Init(GPUSkinningAnimation anim, Mesh mesh, Material mtrl, TextAsset textureRawData) {
        if (player != null) {
            return;
        }

        this.gpuSkinningAnimation = anim;
        this.mesh = mesh;
        this.material = mtrl;
        this.textAsset = textureRawData;
        Init();
    }
#endif
}

