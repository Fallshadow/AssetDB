using UnityEditor.UI;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(EffectImage), true)]
public class EffectImageInspector : ImageEditor {
    private EffectUIInspector _inspector;

    protected override void OnEnable() {
        base.OnEnable();

        if (targets.Length > 1)
            return;

        _inspector = new EffectUIInspector(target as Graphic, this);
    }
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (targets.Length > 1) {
            EditorGUILayout.HelpBox("Special effects not support multi-object editing.", MessageType.None);
            return;
        }

        _inspector.RefreshEffects();
        _inspector.OnInspectorGUI();

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }
}
