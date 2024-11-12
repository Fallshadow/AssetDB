namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {

        public override bool IsActive() {
            return base.IsActive() && m_Content != null;
        }


    }
}