namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }
        [SerializeField] protected RectTransform m_Content;

        public bool horizontal { get { return m_Horizontal; } set { m_Horizontal = value; } }
        [SerializeField] private bool m_Horizontal = true;

        public bool vertical { get { return m_Vertical; } set { m_Vertical = value; } }
        [SerializeField] private bool m_Vertical = true;

        protected enum LoopScrollRectDirection { Vertical, Horizontal }
        protected LoopScrollRectDirection direction = LoopScrollRectDirection.Horizontal;

        // 初始翻转 一开始的时候在最后面
        [Tooltip("Reverse direction for dragging")]
        public bool reverseDirection = false;

        protected float m_ContentLeftPadding = 0;
        protected float m_ContentRightPadding = 0;
        protected float m_ContentTopPadding = 0;
        protected float m_ContentBottomPadding = 0;
        protected GridLayoutGroup m_GridLayout = null;
        private bool m_ContentSpaceInit = false;
        private float m_ContentSpacing = 0;

        // 因为要么是垂直要么是水平，所以 grid 的 spacing 放到各自的 GetAbsDimension 去实现。
        protected float contentSpacing {
            get {
                if (m_ContentSpaceInit) {
                    return m_ContentSpacing;
                }
                m_ContentSpaceInit = true;
                m_ContentSpacing = 0;

                HorizontalOrVerticalLayoutGroup layout1 = m_Content.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (layout1 != null) {
                    m_ContentSpacing = layout1.spacing;
                    m_ContentLeftPadding = layout1.padding.left;
                    m_ContentRightPadding = layout1.padding.right;
                    m_ContentTopPadding = layout1.padding.top;
                    m_ContentBottomPadding = layout1.padding.bottom;
                }
                m_GridLayout = m_Content.GetComponent<GridLayoutGroup>();
                if (m_GridLayout != null) {
                    m_ContentSpacing = GetAbsDimension(m_GridLayout.spacing);
                    m_ContentLeftPadding = m_GridLayout.padding.left;
                    m_ContentRightPadding = m_GridLayout.padding.right;
                    m_ContentTopPadding = m_GridLayout.padding.top;
                    m_ContentBottomPadding = m_GridLayout.padding.bottom;
                }
                return m_ContentSpacing;
            }
        }

        private bool m_ContentConstraintCountInit = false;
        private int m_ContentConstraintCount = 0;
        protected int contentConstraintCount {
            get {
                if (m_ContentConstraintCountInit) {
                    return m_ContentConstraintCount;
                }
                m_ContentConstraintCountInit = true;
                m_ContentConstraintCount = 1;

                GridLayoutGroup layout = m_Content.GetComponent<GridLayoutGroup>();
                if (layout != null) {
                    if (layout.constraint == GridLayoutGroup.Constraint.Flexible) {
                        Debug.LogError("[LoopScrollRect] Flexible not supported yet");
                    }
                    m_ContentConstraintCount = layout.constraintCount;
                }
                return m_ContentConstraintCount;
            }
        }
    }
}
