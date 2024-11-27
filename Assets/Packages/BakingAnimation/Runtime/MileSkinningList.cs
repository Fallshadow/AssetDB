using System.Drawing;
using UnityEngine;

public class MileSkinningList<T> {
    public T[] buffer;
    public T this[int i] {
        get { return buffer[i]; }
        set { buffer[i] = value; }
    }

    public int size = 0;

    private int bufferIncrement = 0;
    public MileSkinningList(int bufferIncrement) {
        this.bufferIncrement = Mathf.Max(1, bufferIncrement);
    }

    public void RemoveAt(int index) {
        if (buffer != null && index > -1 && index < size) {
            --size;
            buffer[index] = default(T);
            for (int b = index; b < size; ++b)
                buffer[b] = buffer[b + 1];
            buffer[size] = default(T);
        }
    }

    public void Release() { size = 0; buffer = null; }
}