using System;

[System.Serializable]
public class Timer {
    public string Name = "";
    public bool canReset = true;
    public bool isGlobal = false;   // 全局Timer，只注册一次，不会被Reset Force清理掉
    public Action cbReset;

    public bool IsUnique { get; private set; }
    public float ElasedTime { get; private set; } = 0f;

    public Timer(string name, bool isUnique) {
        Name = name;
        IsUnique = isUnique;
    }
}