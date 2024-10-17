namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        public Scrollbar horizontalScrollbar {
            get {
                return m_HorizontalScrollbar;
            }
            set {
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
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
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        // �������ɼ��Ե���Ϊ��
        public enum ScrollbarVisibility {
            Permanent,  // ���ǿɼ�
            AutoHide,   // ������Ҫ�ڴ����Ϲ���ʱ���Զ����ع��������ӿھ��β��ᱻ�ı䡣
            /// <summary>
            /// ������Ҫ�ڴ����Ϲ���ʱ�Զ����ع�����������Ӧ����չ�ӿھ���
            /// </summary>
            /// <remarks>
            /// ��ʹ�ô�����ʱ�����������ӿھ��α�����������ζ��RectTransform�е�ֵ���Զ�����ģ������ֶ��༭��
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
    }
}