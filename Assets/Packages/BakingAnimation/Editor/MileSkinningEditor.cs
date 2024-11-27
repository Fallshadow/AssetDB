using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MileSkinning))]
public class MileSkinningEditor : Editor {
    MileSkinning mileSkinning;
    float time = 0;
    string[] clipsName = null;
    public override void OnInspectorGUI() {
        // base.OnInspectorGUI();
        if (mileSkinning == null) {
            mileSkinning = target as MileSkinning;
        }

        ChangeCheck("material");
        ChangeCheck("mesh");
        ChangeCheck("textAsset");
        ChangeCheck("gpuSkinningAnimation");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mileSkinningCullingMode"));
        if (EditorGUI.EndChangeCheck()) {
            if (Application.isPlaying) {
                mileSkinning.Player.MileSkinningCullingMode =
                    serializedObject.FindProperty("mileSkinningCullingMode").enumValueIndex == 0 ? MileSkinningCullingMode.AlwaysAnimate :
                    serializedObject.FindProperty("mileSkinningCullingMode").enumValueIndex == 1 ? MileSkinningCullingMode.CullUpdateTransforms : MileSkinningCullingMode.CullCompletely;
            }
        }

        GPUSkinningAnimation animation = serializedObject.FindProperty("gpuSkinningAnimation").objectReferenceValue as GPUSkinningAnimation;
        SerializedProperty defaultPlayingClipIndex = serializedObject.FindProperty("defaultPlayingClipIndex");
        if (clipsName == null && animation != null) {
            List<string> strings = new List<string>();
            for (int i = 0; i < animation.clips.Length; i++) {
                strings.Add(animation.clips[i].name);
            }
            clipsName = strings.ToArray();
            defaultPlayingClipIndex.intValue = Mathf.Clamp(defaultPlayingClipIndex.intValue, 0, animation.clips.Length);
        }

        if (clipsName != null) {
            EditorGUI.BeginChangeCheck();
            defaultPlayingClipIndex.intValue = EditorGUILayout.Popup("Default Playing", defaultPlayingClipIndex.intValue, clipsName);
            if (EditorGUI.EndChangeCheck()) {
                mileSkinning.Player.Play(clipsName[defaultPlayingClipIndex.intValue]);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ChangeCheck(string prop) {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
            mileSkinning.DeletePlayer();
            mileSkinning.Init();
        }
    }
}