using UnityEngine;

public class MileSkinningPlayer {
    private bool visible = false;
    public bool Visible {
        get { return Application.isPlaying ? visible : true; }
        set { visible = value; }
    }

    MileSkinningCullingMode cullingMode = MileSkinningCullingMode.CullUpdateTransforms;
    public MileSkinningCullingMode CullingMode {
        get { return Application.isPlaying ? cullingMode : MileSkinningCullingMode.AlwaysAnimate; }
        set { cullingMode = value; }
    }

    MileSkinningCullingMode mileSkinningCullingMode = MileSkinningCullingMode.CullUpdateTransforms;
    public MileSkinningCullingMode MileSkinningCullingMode {
        get { return Application.isPlaying ? mileSkinningCullingMode : MileSkinningCullingMode.AlwaysAnimate; }
        set { mileSkinningCullingMode = value; }
    }

    public Vector3 Position { get { return transform == null ? Vector3.zero : transform.position; } }
    private GameObject gameObject;
    private Transform transform;
    private MileSkinningData mileSkinningData;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MaterialPropertyBlock materialPropertyBlock;

    private GPUSkinningClip currentClip;
    private GPUSkinningClip lastPlayedClip;
    private float lastPlayedTime = 0;
    private GPUSkinningClip lastPlayingClip;
    private int lastPlayingFrameIndex = -1;

    private bool isPlaying = false;
    public bool IsPlaying { get { return isPlaying; } }

    public MileSkinningPlayer(GameObject gameObject, MileSkinningData data) {
        this.gameObject = gameObject;
        this.transform = this.gameObject.transform;
        mileSkinningData = data;
        meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        meshFilter = this.gameObject.GetComponent<MeshFilter>();

        MileSkinningMaterial skinningMaterial = GetCurrentMaterial();
        meshRenderer.sharedMaterial = skinningMaterial == null ? null : skinningMaterial.Material;
        meshFilter.sharedMesh = data.mesh;

        materialPropertyBlock = new MaterialPropertyBlock();
    }

    public void Play(int clipIndex) {
        GPUSkinningClip[] clips = mileSkinningData.gpuSkinningAnimation.clips;
        if (clipIndex >= 0 && clipIndex < clips.Length) {
            if (currentClip != clips[clipIndex] || (currentClip != clips[clipIndex] && currentClip != null && currentClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || (currentClip != null && !isPlaying)) {
                SetNewPlayingClip(clips[clipIndex]);
            }
        }
    }

    public void Play(string clipName) {
        GPUSkinningClip[] clips = mileSkinningData.gpuSkinningAnimation.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for (int i = 0; i < numClips; ++i) {
            if (clips[i].name == clipName) {
                if (currentClip != clips[i] || (currentClip != null && currentClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || (currentClip != null && !isPlaying)) {
                    SetNewPlayingClip(clips[i]);
                }
            }
        }
    }

    void SetNewPlayingClip(GPUSkinningClip clip) {
        lastPlayedClip = currentClip;
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        currentClip = clip;
        //rootMotionFrameIndex = -1;
        time = 0;
        timeDiff = Random.Range(0, currentClip.length);
    }

    public bool IsTimeAtTheEndOfLoop {
        get {
            if (currentClip == null) {
                return false;
            }
            else {
                return GetFrameIndex() == ((int)(currentClip.length * currentClip.fps) - 1);
            }
        }
    }

    private int GetFrameIndex() {
        float time = GetCurrentTime();
        if (currentClip.length == time) {
            return GetTheLastFrameIndex_WrapMode_Once(currentClip);
        }
        else {
            return GetFrameIndex_WrapMode_Loop(currentClip, time);
        }
    }

    float time = 0;
    float timeDiff = 0;

    private GPUSkinningWrapMode WrapMode {
        get { return currentClip == null ? GPUSkinningWrapMode.Once : currentClip.wrapMode; }
    }

    private float GetCurrentTime() {
        float time = 0;
        if (WrapMode == GPUSkinningWrapMode.Once) {
            time = this.time;
        }
        else if (WrapMode == GPUSkinningWrapMode.Loop) {
            time = mileSkinningData.Time + (currentClip.individualDifferenceEnabled ? this.timeDiff : 0);
        }
        else {
            throw new System.NotImplementedException();
        }
        return time;
    }

    private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip) {
        return (int)(clip.length * clip.fps) - 1;
    }

    private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time) {
        return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
    }

    private MileSkinningMaterial GetCurrentMaterial() {
        if (mileSkinningData == null) {
            return null;
        }

        return mileSkinningData.GetMaterial();
    }

    Vector4 extraProperty;
    public Vector4 ExtraProperty {
        get { return extraProperty; }
        set { extraProperty = value; }
    }

    public void Update(float timeDelta) {
        Update_Internal(timeDelta);
    }

    void Update_Internal(float timeDelta) {
        if (!isPlaying || currentClip == null) {
            return;
        }

        MileSkinningMaterial currentMaterial = GetCurrentMaterial();
        if (currentMaterial == null) {
            return;
        }

        if (meshRenderer.sharedMaterial != currentMaterial.Material) {
            meshRenderer.sharedMaterial = currentMaterial.Material;
        }

        if (currentClip.wrapMode == GPUSkinningWrapMode.Loop) {
            UpdateMaterial(timeDelta, currentMaterial);
        }
        else if (currentClip.wrapMode == GPUSkinningWrapMode.Once) {
            if (time >= currentClip.length) {
                time = currentClip.length;
                UpdateMaterial(timeDelta, currentMaterial);
            }
            else {
                UpdateMaterial(timeDelta, currentMaterial);
                time += timeDelta;
                if (time > currentClip.length) {
                    time = currentClip.length;
                }
            }
        }
        else {
            throw new System.NotImplementedException();
        }

        lastPlayedTime += timeDelta;
    }

    void UpdateMaterial(float deltaTime, MileSkinningMaterial currentMaterial) {
        int frameIndex = GetFrameIndex();
        if (lastPlayingClip == currentClip && lastPlayingFrameIndex == frameIndex) {
            mileSkinningData.Update(deltaTime, currentMaterial);
            return;
        }

        lastPlayingClip = currentClip;
        lastPlayingFrameIndex = frameIndex;
        GPUSkinningFrame frame = currentClip.frames[frameIndex];
        if (Visible || CullingMode == MileSkinningCullingMode.AlwaysAnimate) {
            mileSkinningData.Update(deltaTime, currentMaterial);
            mileSkinningData.UpdatePlayingData(materialPropertyBlock, currentClip, frameIndex);
            mileSkinningData.UpdateExtraProperty(materialPropertyBlock, extraProperty);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
        UpdateEvents(currentClip, frameIndex);
    }

    private delegate void OnAnimEvent(MileSkinningPlayer player, int eventId);
    private event OnAnimEvent onAnimEvent;

    void UpdateEvents(GPUSkinningClip clip, int frameIndex) {
        UpdateClipEvent(clip, frameIndex);
    }

    void UpdateClipEvent(GPUSkinningClip clip, int frameIndex) {
        if (clip == null || clip.events == null || clip.events.Length == 0) {
            return;
        }

        GPUSkinningAnimEvent[] events = clip.events;
        int numEvents = events.Length;
        for (int i = 0; i < numEvents; ++i) {
            if (events[i].frameIndex == frameIndex && onAnimEvent != null) {
                onAnimEvent(this, events[i].eventId);
                break;
            }
        }
    }


#if UNITY_EDITOR
    public void Update_Editor(float timeDelta) {
        Update_Internal(timeDelta);
    }
#endif

}