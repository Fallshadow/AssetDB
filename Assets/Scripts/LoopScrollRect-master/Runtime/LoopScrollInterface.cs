namespace UnityEngine.UI {
    public interface LoopScrollDataSource {
        void ProvideData(Transform transform, int idx);
    }

    public interface LoopScrollMultiDataSource {
        void ProvideData(Transform transform, int index);
    }

    public interface LoopScrollPrefabSource {
        GameObject GetObject(int index);

        // 将子物体返回池子时进行的操作
        void ReturnObject(Transform trans);
    }
}