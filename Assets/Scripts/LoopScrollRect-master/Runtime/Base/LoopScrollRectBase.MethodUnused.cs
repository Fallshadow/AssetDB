using System.Collections;

namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {
        public void ScrollToCell(int index, float speed) {
            if (totalCount >= 0 && (index < 0 || index >= totalCount)) {
                Debug.LogErrorFormat("invalid index {0}", index);
                return;
            }
            StopAllCoroutines();
            if (speed <= 0) {
                RefillCells(index);
                return;
            }
            StartCoroutine(ScrollToCellCoroutine(index, speed));
        }

        public void ScrollToCellWithinTime(int index, float time) {
            if (totalCount >= 0 && (index < 0 || index >= totalCount)) {
                Debug.LogErrorFormat("invalid index {0}", index);
                return;
            }
            StopAllCoroutines();
            if (time <= 0) {
                RefillCells(index);
                return;
            }
            float dist = 0;
            float offset = 0;
            int currentFirst = reverseDirection ? GetLastItem(out offset) : GetFirstItem(out offset);

            int TargetLine = (index / contentConstraintCount);
            int CurrentLine = (currentFirst / contentConstraintCount);

            if (TargetLine == CurrentLine) {
                dist = offset;
            }
            else {
                if (sizeHelper != null) {
                    dist = GetDimension(sizeHelper.GetItemsSize(currentFirst) - sizeHelper.GetItemsSize(index)) + contentSpacing * (CurrentLine - TargetLine - 1);
                    dist += offset;
                }
                else {
                    float elementSize = (GetAbsDimension(m_ContentBounds.size) - contentSpacing * (CurrentLines - 1)) / CurrentLines;
                    dist = elementSize * (CurrentLine - TargetLine) + contentSpacing * (CurrentLine - TargetLine - 1);
                    dist -= offset;
                }
            }
            StartCoroutine(ScrollToCellCoroutine(index, Mathf.Abs(dist) / time));
        }

        IEnumerator ScrollToCellCoroutine(int index, float speed) {
            bool needMoving = true;
            while (needMoving) {
                yield return null;
                if (!m_Dragging) {
                    float move = 0;
                    if (index < itemTypeStart) {
                        move = -Time.deltaTime * speed;
                    }
                    else if (index >= itemTypeEnd) {
                        move = Time.deltaTime * speed;
                    }
                    else {
                        m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
                        var m_ItemBounds = GetBounds4Item(index);
                        var offset = 0.0f;
                        if (direction == LoopScrollRectDirection.Vertical)
                            offset = reverseDirection ? (m_ViewBounds.min.y - m_ItemBounds.min.y) : (m_ViewBounds.max.y - m_ItemBounds.max.y);
                        else
                            offset = reverseDirection ? (m_ItemBounds.max.x - m_ViewBounds.max.x) : (m_ItemBounds.min.x - m_ViewBounds.min.x);
                        // check if we cannot move on
                        if (totalCount >= 0) {
                            if (offset > 0 && itemTypeEnd == totalCount && !reverseDirection) {
                                m_ItemBounds = GetBounds4Item(totalCount - 1);
                                // reach bottom
                                if ((direction == LoopScrollRectDirection.Vertical && m_ItemBounds.min.y > m_ViewBounds.min.y) ||
                                    (direction == LoopScrollRectDirection.Horizontal && m_ItemBounds.max.x < m_ViewBounds.max.x)) {
                                    needMoving = false;
                                    break;
                                }
                            }
                            else if (offset < 0 && itemTypeStart == 0 && reverseDirection) {
                                m_ItemBounds = GetBounds4Item(0);
                                if ((direction == LoopScrollRectDirection.Vertical && m_ItemBounds.max.y < m_ViewBounds.max.y) ||
                                    (direction == LoopScrollRectDirection.Horizontal && m_ItemBounds.min.x > m_ViewBounds.min.x)) {
                                    needMoving = false;
                                    break;
                                }
                            }
                        }

                        float maxMove = Time.deltaTime * speed;
                        if (Mathf.Abs(offset) < maxMove) {
                            needMoving = false;
                            move = offset;
                        }
                        else
                            move = Mathf.Sign(offset) * maxMove;
                    }
                    if (move != 0) {
                        Vector2 offset = GetVector(move);
                        m_Content.anchoredPosition += offset;
                        m_PrevPosition += offset;
                        m_ContentStartPosition += offset;
                        UpdateBounds(true);
                    }
                }
            }
            StopMovement();
            UpdatePrevData();
        }

        public void ClearCells() {
            if (Application.isPlaying) {
                itemTypeStart = 0;
                itemTypeEnd = 0;
                totalCount = 0;
                for (int i = m_Content.childCount - 1; i >= 0; i--) {
                    prefabSource.ReturnObject(m_Content.GetChild(i));
                }
            }
        }

        /// <summary>
        /// 在结束时重新填充endItem中的单元格，同时清除现有的单元格
        /// </summary>
        public void RefillCellsFromEnd(int endItem = 0, bool alignStart = false) {
            if (!Application.isPlaying)
                return;

            itemTypeEnd = reverseDirection ? endItem : totalCount - endItem;
            itemTypeStart = itemTypeEnd;

            if (totalCount >= 0 && itemTypeStart % contentConstraintCount != 0) {
                itemTypeStart = (itemTypeStart / contentConstraintCount) * contentConstraintCount;
            }

            ReturnToTempPool(!reverseDirection, m_Content.childCount);

            float sizeToFill = GetAbsDimension(viewRect.rect.size), sizeFilled = 0;
            bool first = true;
            // issue 169: fill last line
            if (itemTypeStart < itemTypeEnd) {
                itemTypeEnd = itemTypeStart;
                float size = reverseDirection ? NewItemAtStart(!first) : NewItemAtEnd(!first);
                if (size >= 0) {
                    first = false;
                    sizeFilled += size;
                }
            }

            while (sizeToFill > sizeFilled) {
                float size = reverseDirection ? NewItemAtEnd(!first) : NewItemAtStart(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            // refill from start in case not full yet
            while (sizeToFill > sizeFilled) {
                float size = reverseDirection ? NewItemAtStart(!first) : NewItemAtEnd(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            Vector2 pos = m_Content.anchoredPosition;
            float dist = alignStart ? 0 : Mathf.Max(0, sizeFilled - sizeToFill);
            if (reverseDirection)
                dist = -dist;
            if (direction == LoopScrollRectDirection.Vertical)
                pos.y = dist;
            else
                pos.x = -dist;
            m_Content.anchoredPosition = pos;
            m_ContentStartPosition = pos;

            ClearTempPool();
            // force build bounds here so scrollbar can access newest bounds
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            Canvas.ForceUpdateCanvases();
            UpdateBounds(false);
            UpdateScrollbars(Vector2.zero);
            StopMovement();
            UpdatePrevData();
        }

        public int GetFirstItem(out float offset) {
            if (direction == LoopScrollRectDirection.Vertical)
                offset = m_ViewBounds.max.y - m_ContentBounds.max.y;
            else
                offset = m_ContentBounds.min.x - m_ViewBounds.min.x;
            int idx = 0;
            if (itemTypeEnd > itemTypeStart) {
                float size = GetSize(m_Content.GetChild(0) as RectTransform, false);
                while (size + offset <= 0 && itemTypeStart + idx + contentConstraintCount < itemTypeEnd) {
                    offset += size;
                    idx += contentConstraintCount;
                    size = GetSize(m_Content.GetChild(idx) as RectTransform);
                }
            }
            return idx + itemTypeStart;
        }

        public int GetLastItem(out float offset) {
            if (direction == LoopScrollRectDirection.Vertical)
                offset = m_ContentBounds.min.y - m_ViewBounds.min.y;
            else
                offset = m_ViewBounds.max.x - m_ContentBounds.max.x;
            int idx = 0;
            if (itemTypeEnd > itemTypeStart) {
                int totalChildCount = m_Content.childCount;
                float size = GetSize(m_Content.GetChild(totalChildCount - idx - 1) as RectTransform, false);
                while (size + offset <= 0 && itemTypeStart < itemTypeEnd - idx - contentConstraintCount) {
                    offset += size;
                    idx += contentConstraintCount;
                    size = GetSize(m_Content.GetChild(totalChildCount - idx - 1) as RectTransform);
                }
            }
            offset = -offset;
            return itemTypeEnd - idx - 1;
        }




    }
}