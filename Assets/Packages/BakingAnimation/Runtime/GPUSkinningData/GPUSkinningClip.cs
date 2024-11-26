[System.Serializable]
public enum GPUSkinningWrapMode {
    Once,
    Loop
}

public class GPUSkinningClip {

    public string name = null;

    public float length = 0.0f;

    public int fps = 0;

    public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;
    public GPUSkinningFrame[] frames = null;

    public int pixelSegmentation = 0;

    // 其实就是随机时间控制了同一个 loop 动画不同的退出时间
    public bool individualDifferenceEnabled = false;


    public GPUSkinningAnimEvent[] events = null;
}