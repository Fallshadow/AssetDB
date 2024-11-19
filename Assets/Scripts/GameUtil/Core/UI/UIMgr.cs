using System.Collections.Generic;
using UnityEngine;
using System;

public class UIDefine {
    private static Dictionary<Type, UIDef> m_uiDic;

    public static Dictionary<Type, UIDef> UiDic {
        get {
            if (m_uiDic == null) {
                m_uiDic = new();
                RegisterUI(m_uiDic);
            }
            return m_uiDic;
        }
    }

    private static void RegisterUI(Dictionary<Type, UIDef> dic) {
        Def<LoginUI>(ResKey.LoginUI, UIType.UI);
        //Def<HpSliderUI>("HpSliderUI", UIType.Hud);

        void Def<T>(string key, UIType uiType) where T : UIForm, new() {
            dic.Add(typeof(T), new UIDef() { key = key, uiType = uiType });
        }
    }
}

public class UIMgr : MgrBase, IUpdateListener {
    private Dictionary<Type, UIDef> UiDic => UIDefine.UiDic;
    private readonly List<UIDef> activeUI = new();
    private readonly List<UIDef> updateSchedule = new();

    public override void OnInit() {
        GameObject ctnrObj = GameObject.Instantiate(ResHelper.GetAssetFromCache<GameObject>(ResKey.uiContainer));
        GameObject.DontDestroyOnLoad(ctnrObj);
        GameEntities.globalEnts.uiContainer = ctnrObj.GetComponent<UIContainerObj>();
    }

    public void OnUpdate() {
        updateSchedule.Clear();
        updateSchedule.AddRange(activeUI);

        foreach (UIDef ui in updateSchedule) {
            if (ui.form.enabled) {
                ui.form.OnUpdate();
            }
        }
    }

    public void Open<T>(Action callback = null) where T : UIForm {
        if (!UiDic.TryGetValue(typeof(T), out UIDef def)) {
            Debug.LogError("unregistered ui: " + typeof(T).ToString());
            return;
        }

        if (def.form == null) {
            ResHelper.LoadAsset<GameObject>(def.key, prefab => {
                Transform parent = def.uiType switch {
                    UIType.Hud => GameEntities.globalEnts.uiContainer.hudTrans,
                    _ => GameEntities.globalEnts.uiContainer.uiParent
                };
                GameObject obj = GameObject.Instantiate(prefab, parent);
                def.form = obj.GetComponent<UIForm>();
                def.form.def = def;
                def.form.OnInit();

                BeforeActiveForm(def);
                callback?.Invoke();
            });
        }
        else {
            BeforeActiveForm(def);
            callback?.Invoke();
        }
    }

    private void BeforeActiveForm(UIDef def) {
        def.isActive = true;
        def.form.gameObject.SetActive(true);
        def.form.transform.SetAsLastSibling();
        def.form.OnOpen();
        activeUI.Add(def);
    }

    public void Close(UIForm ui) {
        if (ui.def.isActive) {
            ui.def.isActive = false;
            ui.gameObject.SetActive(false);
            activeUI.Remove(ui.def);
            ui.OnClose();
        }
    }

    public void Close<T>() {
        if (UiDic.TryGetValue(typeof(T), out UIDef ui) && ui.form != null) {
            Close(ui.form);
        }
    }

    public void CloseAll() {
        foreach (UIDef ui in UiDic.Values) {
            if (ui.form != null) {
                Close(ui.form);
            }
        }
    }
}

public class UIForm : MonoBehaviour, UIInterface {
    public UIDef def;

    public virtual void OnInit() {

    }

    public virtual void OnOpen() {

    }

    public virtual void OnClose() {

    }

    public virtual void OnUpdate() {

    }
}

public interface UIInterface {
    public abstract void OnInit();

    public abstract void OnOpen();

    public abstract void OnClose();

    public abstract void OnUpdate();
}

public class UIDef {
    public string key;
    public UIType uiType;

    public UIForm form;
    public bool isActive;


}

public enum UIType {
    Hud,
    UI
}