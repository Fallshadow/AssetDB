using UnityEngine;
using System.Collections;

namespace UnityEngine.UI
{
    public interface LoopScrollPrefabSource
    {
        GameObject GetObject(int index);

        // 将子物体返回池子时进行的操作
        void ReturnObject(Transform trans);
    }
}
