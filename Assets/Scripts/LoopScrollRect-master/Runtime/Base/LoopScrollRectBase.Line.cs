namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {
        // ��һ�� ID
        protected int itemTypeStart = 0;

        // Ŀǰ���һ�� ID
        protected int itemTypeEnd = 0;

        // �����еĵ�һ�С����������һ���а��������Ŀ��
        protected int StartLine {
            get {
                return Mathf.CeilToInt((float)(itemTypeStart) / contentConstraintCount);
            }
        }

        // �����еĵ�ǰ���������������һ���а��������Ŀ��
        protected int CurrentLines {
            get {
                return Mathf.CeilToInt((float)(itemTypeEnd - itemTypeStart) / contentConstraintCount);
            }
        }

        // �����е������������������һ���а��������Ŀ��
        protected int TotalLines {
            get {
                return Mathf.CeilToInt((float)(totalCount) / contentConstraintCount);
            }
        }
    }
}