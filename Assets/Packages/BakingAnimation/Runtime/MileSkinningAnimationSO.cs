using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningAnimation : ScriptableObject {
    public string guid = null;
    public GPUSkinningBone[] bones = null;

    public int textureWidth = 0;
    public int textureHeight = 0;

    public GPUSkinningClip[] clips = null;


    public float sphereRadius = 1.0f;
}