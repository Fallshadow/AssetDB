using UnityEngine;

public class MileSkinningList<T> {
    public T[] buffer;
    public T this[int i] {
        get { return buffer[i]; }
        set { buffer[i] = value; }
    }

    private int bufferIncrement = 0;
    public MileSkinningList(int bufferIncrement) {
        this.bufferIncrement = Mathf.Max(1, bufferIncrement);
    }
}