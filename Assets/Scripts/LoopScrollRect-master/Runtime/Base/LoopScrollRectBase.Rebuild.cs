namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase : ICanvasElement {

#if UNITY_EDITOR
        // 编辑器下更改设置，还是需要进行重建的，有可能改到需要立即生效的设置，比如滑动条的可见性。
        protected override void OnValidate() {
            SetDirtyCaching();
        }
#endif

        // ICanvasElement 重建
        public virtual void Rebuild(CanvasUpdate executing) {
            if (executing == CanvasUpdate.Prelayout) {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout) {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        // ICanvasElement 在布局更新完成后调用。
        public virtual void LayoutComplete() { }

        // ICanvasElement 在图形更新完成后调用。
        public virtual void GraphicUpdateComplete() { }


        // 重写以更改或添加使滚动框的外观与其数据同步的代码。
        protected void SetDirty() {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        // 重写以更改或添加缓存数据的代码，以避免重复的繁重操作。
        protected void SetDirtyCaching() {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        // 如果不在重建，强行重建！
        private void EnsureLayoutHasRebuilt() {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }


        // 重建所需数据
        // 这里需要获取滑动条状态，看看是否需要扩展 viewport
        private void UpdateCachedData() {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect = m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            bool viewIsChild = (viewRect.parent == transform);
            // 有可能不存在，因为有可能只有 v 或者 只有 h
            bool hScrollbarIsChild = (!m_HorizontalScrollbarRect || m_HorizontalScrollbarRect.parent == transform);
            bool vScrollbarIsChild = (!m_VerticalScrollbarRect || m_VerticalScrollbarRect.parent == transform);
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);

            m_HSliderExpand = allAreChildren && m_HorizontalScrollbarRect && horizontalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_VSliderExpand = allAreChildren && m_VerticalScrollbarRect && verticalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_HSliderHeight = (m_HorizontalScrollbarRect == null ? 0 : m_HorizontalScrollbarRect.rect.height);
            m_VSliderWidth = (m_VerticalScrollbarRect == null ? 0 : m_VerticalScrollbarRect.rect.width);
        }
    }
}