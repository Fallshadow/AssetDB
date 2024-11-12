namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        [SerializeField]
        private RectTransform m_Viewport;
        // 对viewport RectTransform的引用，它是 content RectTransform的父元素。
        public RectTransform viewport { 
            get { 
                return m_Viewport; 
            } 
            set { 
                m_Viewport = value; SetDirtyCaching(); 
            } 
        }

        private RectTransform m_ViewRect;

        protected RectTransform viewRect {
            get {
                if (m_ViewRect == null)
                    m_ViewRect = m_Viewport;
                if (m_ViewRect == null)
                    m_ViewRect = (RectTransform)transform;
                return m_ViewRect;
            }
        }

        private Bounds m_ViewBounds;

    }
}