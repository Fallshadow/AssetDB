namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        // �������ƶ���������������Χʱʹ�õ���Ϊ�����á�
        public enum MovementType {
            Unrestricted,   // �������Ƶ��˶������ݿ�����Զ�ƶ���
            Elastic,        // �����˶��������ﱻ������ʱ�Ƴ����������ᱻ���Ե����ء�
            Clamped,        // �����˶������ݲ����ƶ���������֮�⡣
        }

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        // �������ƶ���������֮��ʱʹ�õ���Ϊ��
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private float m_Elasticity = 0.1f;
        // �������ƶ���������֮��ʱҪʹ�õĵ��Դ�С��
        public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }

        [SerializeField]
        private bool m_Inertia = true;
        // �Ƿ�Ӧ�������˶����ԣ�������ζ�Ź��������ڱ��϶�����������һ��ʱ�䡣�����ݼ��������𽥼��١�
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

        [SerializeField]
        private float m_DecelerationRate = 0.135f;
        // �˶��������ٶȡ���������ÿ���Ӽ��ٵ��ٶȡ�0.5��ֵ��ÿ���ٶȵ�һ�롣Ĭ��ֵ��0.135���������ʽ������ù���ʱʹ�á�
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;
        // �Թ��ֺ͹����¼��������ԡ�ֵԽ�߱�ʾ������Խ�ߡ�
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }
    }
}