namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        // 当内容移动到超出其容器范围时使用的行为的设置。
        public enum MovementType {
            Unrestricted,   // 不受限制的运动。内容可以永远移动。
            Elastic,        // 弹性运动。内容物被允许暂时移出容器，但会被弹性地拉回。
            Clamped,        // 限制运动。内容不能移动到其容器之外。
        }

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        // 当内容移动到滚动框之外时使用的行为。
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private float m_Elasticity = 0.1f;
        // 当内容移动到滚动框之外时要使用的弹性大小。
        public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }

        [SerializeField]
        private bool m_Inertia = true;
        // 是否应该启用运动惯性？惯性意味着滚动内容在被拖动后会继续滚动一段时间。它根据减速速率逐渐减速。
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

        [SerializeField]
        private float m_DecelerationRate = 0.135f;
        // 运动减慢的速度。减速率是每秒钟减速的速度。0.5的值是每秒速度的一半。默认值是0.135。减速速率仅在启用惯性时使用。
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;
        // 对滚轮和滚动事件的敏感性。值越高表示灵敏度越高。
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }
    }
}