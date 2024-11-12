namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        [SerializeField]
        private RectTransform m_Viewport;
        // ��viewport RectTransform�����ã����� content RectTransform�ĸ�Ԫ�ء�
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