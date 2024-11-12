namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        public Scrollbar horizontalScrollbar {
            get {
                return m_HorizontalScrollbar;
            }
            set {
                SetHorizontalScrollbarListener(false);
                m_HorizontalScrollbar = value;
                SetHorizontalScrollbarListener(true);
                SetDirtyCaching();
            }
        }

        [SerializeField]
        private Scrollbar m_VerticalScrollbar;
        public Scrollbar verticalScrollbar {
            get {
                return m_VerticalScrollbar;
            }
            set {
                SetVerticalScrollbarListener(false);
                m_VerticalScrollbar = value;
                SetVerticalScrollbarListener(true);
                SetDirtyCaching();
            }
        }

        private RectTransform m_HorizontalScrollbarRect;
        private RectTransform m_VerticalScrollbarRect;

        // 滚动条可见性的行为。
        public enum ScrollbarVisibility {
            Permanent,  // 总是可见
            AutoHide,   // 当不需要在此轴上滚动时，自动隐藏滚动条。视口矩形不会被改变。
            /// <summary>
            /// 当不需要在此轴上滚动时自动隐藏滚动条，并相应地扩展视口矩形
            /// </summary>
            /// <remarks>
            /// 当使用此设置时，滚动条和视口矩形被驱动，这意味着RectTransform中的值是自动计算的，不能手动编辑。
            /// </remarks>
            AutoHideAndExpandViewport,
        }

        [SerializeField]
        private ScrollbarVisibility m_HorizontalScrollbarVisibility;
        public ScrollbarVisibility horizontalScrollbarVisibility { get { return m_HorizontalScrollbarVisibility; } set { m_HorizontalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private ScrollbarVisibility m_VerticalScrollbarVisibility;
        public ScrollbarVisibility verticalScrollbarVisibility { get { return m_VerticalScrollbarVisibility; } set { m_VerticalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private float m_HorizontalScrollbarSpacing;
        public float horizontalScrollbarSpacing { get { return m_HorizontalScrollbarSpacing; } set { m_HorizontalScrollbarSpacing = value; SetDirty(); } }

        [SerializeField]
        private float m_VerticalScrollbarSpacing;
        public float verticalScrollbarSpacing { get { return m_VerticalScrollbarSpacing; } set { m_VerticalScrollbarSpacing = value; SetDirty(); } }


        public void GetHorizonalOffsetAndSize(out float totalSize, out float offset) {
            if (sizeHelper != null) {
                totalSize = sizeHelper.GetItemsSize(TotalLines).x + contentSpacing * (TotalLines - 1);
                offset = m_ContentBounds.min.x - sizeHelper.GetItemsSize(StartLine).x - contentSpacing * StartLine;
            }
            else {
                float elementSize = (m_ContentBounds.size.x - contentSpacing * (CurrentLines - 1)) / CurrentLines;
                totalSize = elementSize * TotalLines + contentSpacing * (TotalLines - 1);
                offset = m_ContentBounds.min.x - elementSize * StartLine - contentSpacing * StartLine;
            }
        }

        public void GetVerticalOffsetAndSize(out float totalSize, out float offset) {
            if (sizeHelper != null) {
                totalSize = sizeHelper.GetItemsSize(TotalLines).y + contentSpacing * (TotalLines - 1);
                offset = m_ContentBounds.max.y + sizeHelper.GetItemsSize(StartLine).y + contentSpacing * StartLine;
            }
            else {
                float elementSize = (m_ContentBounds.size.y - contentSpacing * (CurrentLines - 1)) / CurrentLines;
                totalSize = elementSize * TotalLines + contentSpacing * (TotalLines - 1);
                offset = m_ContentBounds.max.y + elementSize * StartLine + contentSpacing * StartLine;
            }
        }

        private void UpdateScrollbars(Vector2 offset) {
            if (m_HorizontalScrollbar) {
                //==========LoopScrollRect==========
                if (m_ContentBounds.size.x > 0 && totalCount > 0) {
                    float totalSize, _;
                    GetHorizonalOffsetAndSize(out totalSize, out _);
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / totalSize);
                }
                //==========LoopScrollRect==========
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar) {
                //==========LoopScrollRect==========
                if (m_ContentBounds.size.y > 0 && totalCount > 0) {
                    float totalSize, _;
                    GetVerticalOffsetAndSize(out totalSize, out _);
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / totalSize);
                }
                //==========LoopScrollRect==========
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        /// <summary>
        /// 滚动位置为（0,0）和（1,1）之间的Vector2，（0,0）是左下角。
        /// </summary>
        public Vector2 normalizedPosition {
            get {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// 水平滚动位置为介于0和1之间的值，其中0位于左侧。
        /// </summary>
        public float horizontalNormalizedPosition {
            get {
                UpdateBounds();
                //==========LoopScrollRect==========
                if (totalCount > 0 && itemTypeEnd > itemTypeStart) {
                    float totalSize, offset;
                    GetHorizonalOffsetAndSize(out totalSize, out offset);

                    if (totalSize <= m_ViewBounds.size.x)
                        return (m_ViewBounds.min.x > offset) ? 1 : 0;
                    return (m_ViewBounds.min.x - offset) / (totalSize - m_ViewBounds.size.x);
                }
                else
                    return 0.5f;
                //==========LoopScrollRect==========
            }
            set {
                SetNormalizedPosition(value, 0);
            }
        }

        /// <summary>
        /// 垂直滚动位置为介于0和1之间的值，其中0位于底部。
        /// </summary>
        public float verticalNormalizedPosition {
            get {
                UpdateBounds();
                //==========LoopScrollRect==========
                if (totalCount > 0 && itemTypeEnd > itemTypeStart) {
                    float totalSize, offset;
                    GetVerticalOffsetAndSize(out totalSize, out offset);

                    if (totalSize <= m_ViewBounds.size.y)
                        return (offset > m_ViewBounds.max.y) ? 1 : 0;
                    return (offset - m_ViewBounds.max.y) / (totalSize - m_ViewBounds.size.y);
                }
                else
                    return 0.5f;
                //==========LoopScrollRect==========
            }
            set {
                SetNormalizedPosition(value, 1);
            }
        }



        /// <summary>
        /// 将水平或垂直滚动位置设置为0到1之间的值，其中0位于左侧或底部。
        /// </summary>
        /// <param name="value">要设置的位置 0-1</param>
        /// <param name="axis">要设置的轴：0表示水平，1表示垂直.</param>
        protected virtual void SetNormalizedPosition(float value, int axis) {
            if (totalCount <= 0 || itemTypeEnd <= itemTypeStart)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            //==========LoopScrollRect==========
            float totalSize, offset;
            float newAnchoredPosition = m_Content.anchoredPosition[axis];
            if (axis == 0) {
                GetHorizonalOffsetAndSize(out totalSize, out offset);

                if (totalSize >= m_ViewBounds.size.x) {
                    newAnchoredPosition += m_ViewBounds.min.x - value * (totalSize - m_ViewBounds.size.x) - offset;
                }
            }
            else {
                GetVerticalOffsetAndSize(out totalSize, out offset);

                if (totalSize >= m_ViewBounds.size.y) {
                    newAnchoredPosition -= offset - value * (totalSize - m_ViewBounds.size.y) - m_ViewBounds.max.y;
                }
            }

            Vector3 anchoredPosition = m_Content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f) {
                anchoredPosition[axis] = newAnchoredPosition;
                m_Content.anchoredPosition = anchoredPosition;
                m_Velocity[axis] = 0;
                UpdateBounds(true);
            }
        }

        private void SetHorizontalScrollbarListener(bool add) {
            if (m_HorizontalScrollbar) {
                if (add) {
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                }
                else {
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                }
            }
        }
        private void SetVerticalScrollbarListener(bool add) {
            if (m_VerticalScrollbar) {
                if (add) {
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                }
                else {
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                }
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }
    }
}