namespace UnityEngine.UI {

    public abstract partial class LoopScrollRectBase {



        public void RefreshCells() {
            if (Application.isPlaying && this.isActiveAndEnabled) {
                itemTypeEnd = itemTypeStart;
                // recycle items if we can
                for (int i = 0; i < m_Content.childCount; i++) {
                    if (itemTypeEnd < totalCount || totalCount < 0) {
                        ProvideData(m_Content.GetChild(i), itemTypeEnd);
                        itemTypeEnd++;
                    }
                    else {
                        prefabSource.ReturnObject(m_Content.GetChild(i));
                        i--;
                    }
                }
                UpdateBounds(true);
                UpdateScrollbars(Vector2.zero);
            }
        }


        public void RefillCells(int startItem = 0, float contentOffset = 0) {
            if (!Application.isPlaying)
                return;

            itemTypeStart = reverseDirection ? totalCount - startItem : startItem;
            if (totalCount >= 0 && itemTypeStart % contentConstraintCount != 0) {
                itemTypeStart = (itemTypeStart / contentConstraintCount) * contentConstraintCount;
            }
            itemTypeEnd = itemTypeStart;

            // Don't `Canvas.ForceUpdateCanvases();` here, or it will new/delete cells to change itemTypeStart/End
            ReturnToTempPool(reverseDirection, m_Content.childCount);

            float sizeToFill = GetAbsDimension(viewRect.rect.size) + Mathf.Abs(contentOffset);
            float sizeFilled = 0;
            // m_ViewBounds may be not ready when RefillCells on Start

            float itemSize = 0;

            bool first = true;
            while (sizeToFill > sizeFilled) {
                float size = reverseDirection ? NewItemAtStart(!first) : NewItemAtEnd(!first);
                if (size < 0)
                    break;
                first = false;
                itemSize = size;
                sizeFilled += size;
            }

            // refill from start in case not full yet
            while (sizeToFill > sizeFilled) {
                float size = reverseDirection ? NewItemAtEnd(!first) : NewItemAtStart(!first);
                if (size < 0)
                    break;
                first = false;
                sizeFilled += size;
            }

            Vector2 pos = m_Content.anchoredPosition;
            if (direction == LoopScrollRectDirection.Vertical)
                pos.y = -contentOffset;
            else
                pos.x = contentOffset;
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
    }
}