using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GameMgr : MonoBehaviour {
    public static UIMgr ui;
    public static TimeMgr time;

    private readonly List<MgrBase> mgrs = new();
    private readonly List<ModuleBase> modules = new();

    private readonly List<IUpdateListener> updateListeners = new();
    private readonly List<ITickListener> tickListeners = new();

    private bool inited = false;


    void Start() {

        //List<string> urls = new List<string>() {

        //    // ResKey.dmgTextTexture,
        //    //"Prefabs/DropItem/DropItem"
        //};
        //ResHelper.Preload(urls, Init);
    }

    void Init() {
        AddMgrs();
        AddModules();

        foreach (MgrBase mgr in mgrs) {

            mgr.OnInit();

        }

        foreach (ModuleBase md in modules) {

            md.OnInit();

        }

        inited = true;
    }

    void Update() {
        if (!inited) {
            return;
        }

        foreach (IUpdateListener up in updateListeners) {
            //string name = up.GetType().Name;
            //Profiler.BeginSample(name);
            Profiler.BeginSample("update: " + up.GetType().Name);
            up.OnUpdate();
            Profiler.EndSample();
        }

        //if (Time.time >= GameCache.state.nextTickTime) {
        //    GameCache.state.nextTickTime += 0.1f;
        //    foreach (ITickListener tl in tickListeners) {
        //        Profiler.BeginSample("tick: " + tl.GetType().Name);
        //        tl.OnTick();
        //        Profiler.EndSample();
        //    }
        //}
    }

    private void AddMgrs() {
        ui = AddMgr<UIMgr>();
        time = AddMgr<TimeMgr>();
    }

    private void AddModules() {
        //AddModule<BattleBulletModule>();
    }

    private void AddModule<T>() where T : ModuleBase, new() {
        T md = new();
        modules.Add(md);

        if (md is IUpdateListener ul) {
            updateListeners.Add(ul);
        }
        if (md is ITickListener tl) {
            tickListeners.Add(tl);
        }
    }

    private T AddMgr<T>() where T : MgrBase, new() {
        T inst = new();
        mgrs.Add(inst);

        if (inst is IUpdateListener ul) {
            updateListeners.Add(ul);
        }
        if (inst is ITickListener tl) {
            tickListeners.Add(tl);
        }

        return inst;
    }
}

public abstract class ModuleBase {
    public virtual void OnInit() {

    }
}

public abstract class MgrBase {
    public virtual void OnInit() {

    }
}

public interface IUpdateListener {
    public void OnUpdate();
}

public interface ITickListener {
    public void OnTick();
}