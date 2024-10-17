namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        protected enum LoopScrollRectDirection { Vertical, Horizontal }
        protected LoopScrollRectDirection direction = LoopScrollRectDirection.Horizontal;

        // ·½Ïò·­×ª£¿
        [Tooltip("Reverse direction for dragging")]
        public bool reverseDirection = false;

        [SerializeField]
        private bool m_Horizontal = true;
        public bool horizontal { get { return m_Horizontal; } set { m_Horizontal = value; } }

        [SerializeField]
        private bool m_Vertical = true;
        public bool vertical { get { return m_Vertical; } set { m_Vertical = value; } }
    }
}