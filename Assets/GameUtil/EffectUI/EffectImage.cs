using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class EffectImage : Image {
    [SerializeField]
    public List<EffectUIPlayer> EffectsPlayers = new List<EffectUIPlayer>();

    public bool IsUnscaledDeltaTime = false;

    protected override void Start() {
        base.Start();

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        for (int i = 0; i < EffectsPlayers.Count; i++) {
            EffectsPlayers[i].OnStart();
        }
    }
    private void Update() {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        if (EffectsPlayers.Count == 0)
            return;

        for (int i = 0; i < EffectsPlayers.Count; i++) {
            if (IsUnscaledDeltaTime) {
                EffectsPlayers[i].OnUpdate(Time.unscaledDeltaTime);
            }
            else {
                EffectsPlayers[i].OnUpdate(Time.deltaTime);
            }

        }
    }

    /// <summary>
    /// 获取特效播放器
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>特效播放器</returns>
    public EffectUIPlayer GetEffectsPlayer(string propertyName) {
        return EffectsPlayers.Find((e) => { return e.PropertyName == propertyName; });
    }
    /// <summary>
    /// 设置特效播放器的播放状态
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="playing">播放状态</param>
    public void SetEffectsPlaying(string propertyName, bool playing) {
        EffectUIPlayer player = EffectsPlayers.Find((e) => { return e.PropertyName == propertyName; });
        if (player != null) {
            player.IsPlaying = playing;
        }
    }
    /// <summary>
    /// 设置特效播放器的播放位置（0-1之间）
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="position">播放位置</param>
    public void SetEffectsPlayPosition(string propertyName, float position) {
        EffectUIPlayer player = EffectsPlayers.Find((e) => { return e.PropertyName == propertyName; });
        if (player != null) {
            player.PlayPosition = position;
        }
    }

    public void SetAllEffectsPlayPosition(float position) {
        foreach (var e in EffectsPlayers) {
            if (e != null) {
                e.PlayPosition = position;
            }
        }
    }

    public void SetAllEffectsPlaying(bool playing) {
        foreach (var e in EffectsPlayers) {
            if (e != null) {
                e.IsPlaying = playing;
            }
        }
    }
}
