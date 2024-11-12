using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace UnityEngine.UI {
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// 用于创建具有可重用内容的子 RectTransform 滚动。
    /// </summary>
    /// <remarks>
    /// LoopScrollRect 自己不会做任何 clip。结合Mask组件，它可以变成一个循环滚动视图。
    /// </remarks>
    public abstract partial class LoopScrollRectBase : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ILayoutElement, ILayoutGroup {
        // 用于填充项的滚动数据源。
        [HideInInspector]
        [NonSerialized]
        public LoopScrollPrefabSource prefabSource = null;

        // 卷轴中id为[0，totalCount]的项目的总数。负值表示有无限项，如-1。
        [Tooltip("项目的总数。负值表示有无限项，如-1。")]
        public int totalCount;

        // [可选] 帮助准确的大小，这样我们可以实现更好的滚动。
        [HideInInspector]
        [NonSerialized]
        public LoopScrollSizeHelper sizeHelper = null;

        // 当达到阈值时，我们在视图外准备新的项目。这将被扩展到至少 1.5 * itemSize。
        protected float threshold = 0;

        protected abstract float GetSize(RectTransform item, bool includeSpacing = true);
        protected abstract float GetDimension(Vector2 vector);
        protected abstract float GetAbsDimension(Vector2 vector);
        protected abstract Vector2 GetVector(float value);


        protected virtual bool UpdateItems(ref Bounds viewBounds, ref Bounds contentBounds) { return false; }

        

        

        // ScrollRect使用的事件类型
        [Serializable]
        public class ScrollRectEvent : UnityEvent<Vector2> { }

        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();
        // 监视ScrollRect对象中的更改，crollRect对象中子物体的位置改变时执行的回调。
        public ScrollRectEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // 指针位置和点击位置两者的偏移
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        protected Bounds m_ContentBounds;


        private Vector2 m_Velocity;
        // 内容的当前速度。The velocity is defined in units per second.
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        private bool m_Dragging;
        private bool m_Scrolling;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        private bool m_HSliderExpand;
        private bool m_VSliderExpand;
        private float m_HSliderHeight;
        private float m_VSliderWidth;

        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform {
            get {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }



        private DrivenRectTransformTracker m_Tracker;

#if UNITY_EDITOR
        protected override void Awake() {
            base.Awake();
            if (Application.isPlaying) {
                // 正常情况下 水平方向为 0，垂直方向为 1
                // 翻转情况下 水平方向为 1，垂直方向为 0
                float value = (reverseDirection ^ (direction == LoopScrollRectDirection.Horizontal)) ? 0 : 1;

                // 确保 content 锚点正常
                if (m_Content != null) {
                    Debug.Assert(GetAbsDimension(m_Content.pivot) == value, $"翻转：{reverseDirection} 方向：{direction} 对比值：{value} pivot 异常",this);
                    Debug.Assert(GetAbsDimension(m_Content.anchorMin) == value, $"翻转：{reverseDirection} 方向：{direction} 对比值：{value} anchorMin 异常", this);
                    Debug.Assert(GetAbsDimension(m_Content.anchorMax) == value, $"翻转：{reverseDirection} 方向：{direction} 对比值：{value} anchorMax 异常", this);
                }

                if (direction == LoopScrollRectDirection.Vertical)
                    Debug.Assert(m_Vertical && !m_Horizontal, this);
                else
                    Debug.Assert(!m_Vertical && m_Horizontal, this);
            }
        }
#endif
        protected override void OnEnable() {
            base.OnEnable();

            SetHorizontalScrollbarListener(true);
            SetVerticalScrollbarListener(true);

            SetDirtyCaching();
        }

        protected override void OnDisable() {
            SetHorizontalScrollbarListener(false);
            SetVerticalScrollbarListener(false);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }


        protected abstract void ProvideData(Transform transform, int index);

        protected float NewItemAtStart(bool includeSpacing = true) {
            if (totalCount >= 0 && itemTypeStart - contentConstraintCount < 0) {
                return -1;
            }
            float size = 0;
            for (int i = 0; i < contentConstraintCount; i++) {
                itemTypeStart--;
                RectTransform newItem = GetFromTempPool(itemTypeStart);
                newItem.SetSiblingIndex(deletedItemTypeStart);
                size = Mathf.Max(GetSize(newItem, includeSpacing), size);
            }
            threshold = Mathf.Max(threshold, size * 1.5f);

            if (size > 0) {
                m_HasRebuiltLayout = false;
                if (!reverseDirection) {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition += offset;
                    m_PrevPosition += offset;
                    m_ContentStartPosition += offset;
                }
            }

            return size;
        }

        protected float DeleteItemAtStart() {
            // special case: when moving or dragging, we cannot simply delete start when we've reached the end
            if ((m_Dragging || m_Velocity != Vector2.zero) && totalCount >= 0 && itemTypeEnd >= totalCount - contentConstraintCount) {
                return 0;
            }
            int availableChilds = m_Content.childCount - deletedItemTypeStart - deletedItemTypeEnd;
            Debug.Assert(availableChilds >= 0);
            if (availableChilds == 0) {
                return 0;
            }

            float size = 0;
            for (int i = 0; i < contentConstraintCount; i++) {
                RectTransform oldItem = m_Content.GetChild(deletedItemTypeStart) as RectTransform;
                size = Mathf.Max(GetSize(oldItem), size);
                ReturnToTempPool(true);
                availableChilds--;
                itemTypeStart++;

                if (availableChilds == 0) {
                    break;
                }
            }

            if (size > 0) {
                m_HasRebuiltLayout = false;
                if (!reverseDirection) {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition -= offset;
                    m_PrevPosition -= offset;
                    m_ContentStartPosition -= offset;
                }
            }

            return size;
        }

        protected float NewItemAtEnd(bool includeSpacing = true) {
            if (totalCount >= 0 && itemTypeEnd >= totalCount) {
                return -1;
            }
            float size = 0;
            // issue 4: fill lines to end first
            int availableChilds = m_Content.childCount - deletedItemTypeStart - deletedItemTypeEnd;
            int count = contentConstraintCount - (availableChilds % contentConstraintCount);
            for (int i = 0; i < count; i++) {
                RectTransform newItem = GetFromTempPool(itemTypeEnd);
                newItem.SetSiblingIndex(m_Content.childCount - deletedItemTypeEnd - 1);
                size = Mathf.Max(GetSize(newItem, includeSpacing), size);
                itemTypeEnd++;
                if (totalCount >= 0 && itemTypeEnd >= totalCount) {
                    break;
                }
            }
            threshold = Mathf.Max(threshold, size * 1.5f);

            if (size > 0) {
                m_HasRebuiltLayout = false;
                if (reverseDirection) {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition -= offset;
                    m_PrevPosition -= offset;
                    m_ContentStartPosition -= offset;
                }
            }

            return size;
        }

        protected float DeleteItemAtEnd() {
            if ((m_Dragging || m_Velocity != Vector2.zero) && totalCount >= 0 && itemTypeStart < contentConstraintCount) {
                return 0;
            }
            int availableChilds = m_Content.childCount - deletedItemTypeStart - deletedItemTypeEnd;
            Debug.Assert(availableChilds >= 0);
            if (availableChilds == 0) {
                return 0;
            }

            float size = 0;
            for (int i = 0; i < contentConstraintCount; i++) {
                RectTransform oldItem = m_Content.GetChild(m_Content.childCount - deletedItemTypeEnd - 1) as RectTransform;
                size = Mathf.Max(GetSize(oldItem), size);
                ReturnToTempPool(false);
                availableChilds--;
                itemTypeEnd--;
                if (itemTypeEnd % contentConstraintCount == 0 || availableChilds == 0) {
                    break;  //just delete the whole row
                }
            }

            if (size > 0) {
                m_HasRebuiltLayout = false;
                if (reverseDirection) {
                    Vector2 offset = GetVector(size);
                    m_Content.anchoredPosition += offset;
                    m_PrevPosition += offset;
                    m_ContentStartPosition += offset;
                }
            }

            return size;
        }







        /// <summary>
        /// 将两个轴上的速度设置为零，以便内容停止移动。
        /// </summary>
        public virtual void StopMovement() {
            m_Velocity = Vector2.zero;
        }


        /// <summary>
        /// 设置内容的锚定位置
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position) {
            if (!m_Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (!m_Vertical)
                position.y = m_Content.anchoredPosition.y;

            //==========LoopScrollRect==========
            if ((position - m_Content.anchoredPosition).sqrMagnitude > 0.001f) {
                m_Content.anchoredPosition = position;
                UpdateBounds(true);
            }
            //==========LoopScrollRect==========
        }

        protected virtual void LateUpdate() {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero)) {
                Vector2 position = m_Content.anchoredPosition;
                for (int axis = 0; axis < 2; axis++) {
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (m_MovementType == MovementType.Elastic && offset[axis] != 0) {
                        float speed = m_Velocity[axis];
                        float smoothTime = m_Elasticity;
                        if (m_Scrolling)
                            smoothTime *= 3.0f;
                        position[axis] = Mathf.SmoothDamp(m_Content.anchoredPosition[axis], m_Content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                        if (Mathf.Abs(speed) < 1)
                            speed = 0;
                        m_Velocity[axis] = speed;
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (m_Inertia) {
                        m_Velocity[axis] *= Mathf.Pow(m_DecelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity[axis]) < 1)
                            m_Velocity[axis] = 0;
                        position[axis] += m_Velocity[axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else {
                        m_Velocity[axis] = 0;
                    }
                }

                if (m_MovementType == MovementType.Clamped) {
                    offset = CalculateOffset(position - m_Content.anchoredPosition);
                    position += offset;
                }

                SetContentAnchoredPosition(position);
            }

            if (m_Dragging && m_Inertia) {
                Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition) {
                UpdateScrollbars(offset);
#if UNITY_2017_1_OR_NEWER
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
#endif
                m_OnValueChanged.Invoke(normalizedPosition);
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            m_Scrolling = false;
        }

        /// <summary>
        /// 用于更新ScrollRect上的先前数据字段。在更改ScrollRect中的数据之前调用这个函数。
        /// </summary>
        protected void UpdatePrevData() {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }


        private static float RubberDelta(float overStretching, float viewSize) {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange() {
            SetDirty();
        }

        private bool hScrollingNeeded {
            get {
                if (Application.isPlaying)
                    return m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
                return true;
            }
        }

        private bool vScrollingNeeded {
            get {
                if (Application.isPlaying)
                    return m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;
                return true;
            }
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }

        public virtual void SetLayoutHorizontal() {
            m_Tracker.Clear();

            if (m_HSliderExpand || m_VSliderExpand) {
                m_Tracker.Add(this, viewRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // Make view full size to see if content fits.
                viewRect.anchorMin = Vector2.zero;
                viewRect.anchorMax = Vector2.one;
                viewRect.sizeDelta = Vector2.zero;
                viewRect.anchoredPosition = Vector2.zero;

                // Recalculate content layout with this size to see if it fits when there are no scrollbars.
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded) {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);

                // Recalculate content layout with this size to see if it fits vertically
                // when there is a vertical scrollbar (which may reflowed the content to make it taller).
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit horizontally, enable horizontal scrollbar and shrink view vertically to make room for it.
            if (m_HSliderExpand && hScrollingNeeded) {
                viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If the vertical slider didn't kick in the first time, and the horizontal one did,
            // we need to check again if the vertical slider now needs to kick in.
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded && viewRect.sizeDelta.x == 0 && viewRect.sizeDelta.y < 0) {
                viewRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), viewRect.sizeDelta.y);
            }
        }

        public virtual void SetLayoutVertical() {
            UpdateScrollbarLayout();
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();
        }

        void UpdateScrollbarVisibility() {
            UpdateOneScrollbarVisibility(vScrollingNeeded, m_Vertical, m_VerticalScrollbarVisibility, m_VerticalScrollbar);
            UpdateOneScrollbarVisibility(hScrollingNeeded, m_Horizontal, m_HorizontalScrollbarVisibility, m_HorizontalScrollbar);
        }

        private static void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, ScrollbarVisibility scrollbarVisibility, Scrollbar scrollbar) {
            if (scrollbar) {
                if (scrollbarVisibility == ScrollbarVisibility.Permanent) {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                }
                else {
                    if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                        scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
        }

        void UpdateScrollbarLayout() {
            if (m_VSliderExpand && m_HorizontalScrollbar) {
                m_Tracker.Add(this, m_HorizontalScrollbarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);
                m_HorizontalScrollbarRect.anchorMin = new Vector2(0, m_HorizontalScrollbarRect.anchorMin.y);
                m_HorizontalScrollbarRect.anchorMax = new Vector2(1, m_HorizontalScrollbarRect.anchorMax.y);
                m_HorizontalScrollbarRect.anchoredPosition = new Vector2(0, m_HorizontalScrollbarRect.anchoredPosition.y);
                if (vScrollingNeeded)
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_HorizontalScrollbarRect.sizeDelta.y);
                else
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(0, m_HorizontalScrollbarRect.sizeDelta.y);
            }

            if (m_HSliderExpand && m_VerticalScrollbar) {
                m_Tracker.Add(this, m_VerticalScrollbarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);
                m_VerticalScrollbarRect.anchorMin = new Vector2(m_VerticalScrollbarRect.anchorMin.x, 0);
                m_VerticalScrollbarRect.anchorMax = new Vector2(m_VerticalScrollbarRect.anchorMax.x, 1);
                m_VerticalScrollbarRect.anchoredPosition = new Vector2(m_VerticalScrollbarRect.anchoredPosition.x, 0);
                if (hScrollingNeeded)
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                else
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// 计算ScrollRect应该使用的边界。
        /// </summary>
        protected void UpdateBounds(bool updateItems = false)
        {
            m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            m_ContentBounds = GetBounds();

            // Don't do this in Rebuild. Make use of ContentBounds before Adjust here.
            if (Application.isPlaying && updateItems && UpdateItems(ref m_ViewBounds, ref m_ContentBounds)) {
                EnsureLayoutHasRebuilt();
                m_ContentBounds = GetBounds();
            }

            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            var contentPivot = m_Content.pivot;
            AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;

            if (movementType == MovementType.Clamped) {
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to InternalUpdateBounds above).
                Vector2 delta = Vector2.zero;
                if (m_ViewBounds.max.x > m_ContentBounds.max.x) {
                    delta.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }
                else if (m_ViewBounds.min.x < m_ContentBounds.min.x) {
                    delta.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
                }

                if (m_ViewBounds.min.y < m_ContentBounds.min.y) {
                    delta.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                else if (m_ViewBounds.max.y > m_ContentBounds.max.y) {
                    delta.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon) {
                    contentPos = m_Content.anchoredPosition + delta;
                    if (!m_Horizontal)
                        contentPos.x = m_Content.anchoredPosition.x;
                    if (!m_Vertical)
                        contentPos.y = m_Content.anchoredPosition.y;
                    AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos) {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0) {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0) {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        // 获取 content 的四个角的世界坐标，转换到 viewRect 本地坐标下。
        // 生成 content 在 viewRect 下的 Bounds
        private Bounds GetBounds() {
            m_Content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix) {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++) {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        //==========LoopScrollRect==========
        private Bounds GetBounds4Item(int index) {
            int offset = index - itemTypeStart;
            if (offset < 0 || offset >= m_Content.childCount)
                return new Bounds();

            var rt = m_Content.GetChild(offset) as RectTransform;
            if (rt == null)
                return new Bounds();
            rt.GetWorldCorners(m_Corners);

            var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }
        //==========LoopScrollRect==========

        private Vector2 CalculateOffset(Vector2 delta) {
            //==========LoopScrollRect==========
            if (totalCount < 0 || movementType == MovementType.Unrestricted)
                return delta;

            Bounds contentBound = m_ContentBounds;
            if (m_Horizontal) {
                float totalSize, offset;
                GetHorizonalOffsetAndSize(out totalSize, out offset);

                Vector3 center = contentBound.center;
                center.x = offset;
                contentBound.Encapsulate(center);
                center.x = offset + totalSize;
                contentBound.Encapsulate(center);
            }
            if (m_Vertical) {
                float totalSize, offset;
                GetVerticalOffsetAndSize(out totalSize, out offset);

                Vector3 center = contentBound.center;
                center.y = offset;
                contentBound.Encapsulate(center);
                center.y = offset - totalSize;
                contentBound.Encapsulate(center);
            }
            //==========LoopScrollRect==========
            return InternalCalculateOffset(ref m_ViewBounds, ref contentBound, m_Horizontal, m_Vertical, m_MovementType, ref delta);
        }

        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta) {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal) {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical) {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }
    }
}