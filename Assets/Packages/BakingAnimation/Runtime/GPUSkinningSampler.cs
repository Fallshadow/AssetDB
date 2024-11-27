using UnityEngine;
using System.Collections.Generic;
using System.IO;



#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler : MonoBehaviour {
#if UNITY_EDITOR



    [HideInInspector]
    [System.NonSerialized]
    public int samplingClipIndex = -1;

    public void BeginSample() {
        samplingClipIndex = 0;
    }

    public void EndSample() {
        samplingClipIndex = -1;
    }


    private new Animation animation = null;

    private Animator animator = null;
    private RuntimeAnimatorController runtimeAnimatorController = null;

    [HideInInspector]
    [SerializeField]
    public AnimationClip[] animClips = null;

    private void Awake() {
        animation = GetComponent<Animation>();
        animator = GetComponent<Animator>();

        if (animator == null && animation == null) {
            DestroyImmediate(this);
            ShowDialog("Cannot find Animator Or Animation Component");
            return;
        }

        if (animator != null && animation != null) {
            DestroyImmediate(this);
            ShowDialog("Animation is not coexisting with Animator");
            return;
        }

        if (animator != null) {
            if (animator.runtimeAnimatorController == null) {
                DestroyImmediate(this);
                ShowDialog("Missing RuntimeAnimatorController");
                return;
            }

            if (animator.runtimeAnimatorController is AnimatorOverrideController) {
                DestroyImmediate(this);
                ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
                return;
            }

            runtimeAnimatorController = animator.runtimeAnimatorController;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            InitTransform();
            return;
        }

        if (animation != null) {
            MappingAnimationClips();
            animation.Stop();
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            InitTransform();
            return;
        }
    }

    [HideInInspector]
    [System.NonSerialized]
    public bool isSampling = false;
    [HideInInspector]
    [System.NonSerialized]
    public int samplingTotalFrams = 0;
    [HideInInspector]
    [System.NonSerialized]
    public int samplingFrameIndex = 0;
    [HideInInspector]
    [SerializeField]
    public GPUSkinningAnimation anim = null;
    [HideInInspector]
    [SerializeField]
    public string animName = null;

    private GPUSkinningClip gpuSkinningClip = null;

    private void Update() {
        if (!isSampling) {
            return;
        }

        int totalFrames = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
        samplingTotalFrams = totalFrames;

        if (samplingFrameIndex >= totalFrames) {
            if (animator != null) {
                animator.StopPlayback();
            }

            string savePath = null;
            if (anim == null) {
                savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), animName);
            }
            else {
                string animPath = AssetDatabase.GetAssetPath(anim);
                savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
            }

            if (!string.IsNullOrEmpty(savePath)) {
                if (!savePath.Contains(Application.dataPath.Replace('\\', '/'))) {
                    ShowDialog("Must select a directory in the project's Asset folder.");
                }
                else {

                }
            }

        }
    }

    private void InitTransform() {
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
    }

    public void MappingAnimationClips() {
        if (animation == null) {
            return;
        }

        List<AnimationClip> newClips = null;
        AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
        if (clips != null) {
            for (int i = 0; i < clips.Length; ++i) {
                AnimationClip clip = clips[i];
                if (clip != null) {
                    if (animClips == null || System.Array.IndexOf(animClips, clip) == -1) {
                        if (newClips == null) {
                            newClips = new List<AnimationClip>();
                        }
                        newClips.Clear();
                        if (animClips != null)
                            newClips.AddRange(animClips);
                        newClips.Add(clip);
                        animClips = newClips.ToArray();
                    }
                }
            }
        }
    }


    public static void ShowDialog(string msg) {
        EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
    }

    private string GetUserPreferDir() {
        return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
    }
#endif
}