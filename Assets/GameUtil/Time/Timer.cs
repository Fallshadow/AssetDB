using System;

[Serializable]
public class Timer {
    public string Name = "";
    public string GroupName;
    public float ElasedTime { get; private set; } = 0f;
    public bool canReset = true;
    public bool isGlobal = false;   //全局Timer,  只注册一次， 不会被Reset Force清理掉
    public Action cbReset;

    public bool IsUnique { get; private set; }
    public bool IsUnscaled { get; private set; }
    protected bool pause { get; set; } = false;

    public Timer(string name, bool isUnique, bool isUnscaled, Action reset = null) {
        Name = name;
        IsUnique = isUnique;
        IsUnscaled = isUnscaled;
        cbReset = reset;
    }

    public virtual void Pause() {
        pause = true;
    }

    public virtual void Play() {
        Reset();
        pause = false;
    }

    public virtual void Continue() {
        pause = false;
    }

    public virtual void Reset() {
        ElasedTime = 0;
        cbReset?.Invoke();
    }

    public virtual bool IsPlaying() {
        return !pause;
    }

    public virtual void Process(float deltaTime) {
        if (pause)
            return;

        ElasedTime += deltaTime;
    }

    public float GetElasedTime() {
        return ElasedTime;
    }
}