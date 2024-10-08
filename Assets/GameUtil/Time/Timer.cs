using System;

[System.Serializable]
public class Timer {
    public string Name = "";
    public bool canReset = true;
    public bool isGlobal = false;   // ȫ��Timer��ֻע��һ�Σ����ᱻReset Force�����
    public Action cbReset;

    public bool IsUnique { get; private set; }
    public float ElasedTime { get; private set; } = 0f;

    public Timer(string name, bool isUnique) {
        Name = name;
        IsUnique = isUnique;
    }
}