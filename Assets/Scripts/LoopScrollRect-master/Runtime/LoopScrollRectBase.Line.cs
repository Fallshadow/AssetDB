namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {
        // 第一个 ID
        protected int itemTypeStart = 0;

        // 目前最后一个 ID
        protected int itemTypeEnd = 0;

        // 滚动中的第一行。网格可以在一行中包含多个项目。
        protected int StartLine {
            get {
                return Mathf.CeilToInt((float)(itemTypeStart) / contentConstraintCount);
            }
        }

        // 滚动中的当前行数。网格可以在一行中包含多个项目。
        protected int CurrentLines {
            get {
                return Mathf.CeilToInt((float)(itemTypeEnd - itemTypeStart) / contentConstraintCount);
            }
        }

        // 滚动中的总行数。网格可以在一行中包含多个项目。
        protected int TotalLines {
            get {
                return Mathf.CeilToInt((float)(totalCount) / contentConstraintCount);
            }
        }
    }
}