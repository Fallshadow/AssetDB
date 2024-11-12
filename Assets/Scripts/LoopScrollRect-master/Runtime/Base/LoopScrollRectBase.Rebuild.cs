namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase : ICanvasElement {

#if UNITY_EDITOR
        // �༭���¸������ã�������Ҫ�����ؽ��ģ��п��ܸĵ���Ҫ������Ч�����ã����绬�����Ŀɼ��ԡ�
        protected override void OnValidate() {
            SetDirtyCaching();
        }
#endif

        // ICanvasElement �ؽ�
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

        // ICanvasElement �ڲ��ָ�����ɺ���á�
        public virtual void LayoutComplete() { }

        // ICanvasElement ��ͼ�θ�����ɺ���á�
        public virtual void GraphicUpdateComplete() { }


        // ��д�Ը��Ļ����ʹ������������������ͬ���Ĵ��롣
        protected void SetDirty() {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        // ��д�Ը��Ļ���ӻ������ݵĴ��룬�Ա����ظ��ķ��ز�����
        protected void SetDirtyCaching() {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        // ��������ؽ���ǿ���ؽ���
        private void EnsureLayoutHasRebuilt() {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }


        // �ؽ���������
        // ������Ҫ��ȡ������״̬�������Ƿ���Ҫ��չ viewport
        private void UpdateCachedData() {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect = m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            bool viewIsChild = (viewRect.parent == transform);
            // �п��ܲ����ڣ���Ϊ�п���ֻ�� v ���� ֻ�� h
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