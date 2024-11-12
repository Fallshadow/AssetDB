namespace UnityEngine.UI {
    public abstract partial class LoopScrollRectBase {
        protected int deletedItemTypeStart = 0;
        protected int deletedItemTypeEnd = 0;
        protected abstract RectTransform GetFromTempPool(int itemIdx);
        protected abstract void ReturnToTempPool(bool fromStart, int count = 1);
        protected abstract void ClearTempPool();
    }
}