namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }
        [SerializeField]
        protected RectTransform m_Content;
        protected float m_ContentLeftPadding = 0;
        protected float m_ContentRightPadding = 0;
        protected float m_ContentTopPadding = 0;
        protected float m_ContentBottomPadding = 0;
        protected GridLayoutGroup m_GridLayout = null;
        private bool m_ContentSpaceInit = false;
        private float m_ContentSpacing = 0;

        protected float contentSpacing {
            get {
                if (m_ContentSpaceInit) {
                    return m_ContentSpacing;
                }
                m_ContentSpaceInit = true;
                m_ContentSpacing = 0;
                if (m_Content != null) {
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
                if (m_Content != null) {
                    GridLayoutGroup layout2 = m_Content.GetComponent<GridLayoutGroup>();
                    if (layout2 != null) {
                        if (layout2.constraint == GridLayoutGroup.Constraint.Flexible) {
                            Debug.LogError("[LoopScrollRect] Flexible not supported yet");
                        }
                        m_ContentConstraintCount = layout2.constraintCount;
                    }
                }
                return m_ContentConstraintCount;
            }
        }
    }
}
