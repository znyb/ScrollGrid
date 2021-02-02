using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ScrollGrid : ContentSizeFitGrid
{
    public RectOffset myVisiblePadding = new RectOffset();
    public event Action<int, RectTransform> OnFillItem;
    public event Action<int, RectTransform> OnAfterCacheItem;

    public event Func<int,RectTransform> OnGetItem;
    public event Action<int, RectTransform> OnCacheItem;

    public event Action OnVisibleItemUpdate;

    ScrollRect myScrollRect;
    ScrollItemPool myItemPool;

    int myCount;
    List<Vector2> myCellsPosition = new List<Vector2>();
    
    Dictionary<int, RectTransform> myVisibleItems = new Dictionary<int, RectTransform>();

    List<int> myVisibleItemIndex = new List<int>();
    Dictionary<int, RectTransform> myCacheVisibleItems = new Dictionary<int, RectTransform>();

    public Vector2 Size
    {
        get
        {
            return rectTransform.rect.size;
        }
    }

    public int MyCount
    {
        get
        {
            return myCount;
        }

        set
        {
            if (myCount != value)
            {
                // 记录调整尺寸前的位置
                var preSize = rectTransform.sizeDelta;
                var nPos = MyScrollRect.normalizedPosition;

                myCount = value;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                CalculateCellsPosition();

                // 调整normalizedPosition，确保前面的物件位置不变
                if (MyScrollRect.vertical)
                {
                    if (startCorner == Corner.UpperLeft || startCorner == Corner.UpperRight)
                    {
                        var realPos = (1 - nPos.y) * preSize.y;
                        MyScrollRect.verticalNormalizedPosition = 1 - realPos / rectTransform.sizeDelta.y;
                    }
                    else
                    {
                        var realPos = nPos.y * preSize.y;
                        MyScrollRect.verticalNormalizedPosition = realPos / rectTransform.sizeDelta.y;
                    }
                }
                if (myScrollRect.horizontal)
                {
                    if (startCorner == Corner.LowerLeft || startCorner == Corner.UpperLeft)
                    {
                        var realPos = nPos.x * preSize.x;
                        MyScrollRect.horizontalNormalizedPosition = realPos / rectTransform.sizeDelta.x;
                    }
                    else
                    {
                        var realPos = (1 - nPos.x) * preSize.x;
                        MyScrollRect.horizontalNormalizedPosition = 1 - realPos / rectTransform.sizeDelta.x;
                    }
                }
            }

            //可能个数没变，但是内容变了，强制刷新
            RefreshGrid();
        }
    }

    /// <summary>
    /// 将指定index的对象放到可见物件中间
    /// </summary>
    public void CenterItem(int index)
    {
        var pos = CalcItemNormalizedPos(index);
        MyScrollRect.normalizedPosition = pos;

        //RefreshGrid();
    }

    public Vector2 CalcItemNormalizedPos(int index)
    {
        index = Mathf.Clamp(index, 0, myCellsPosition.Count - 1);
        var pos = myCellsPosition[index];
        Vector2 nPos = new Vector2();
        if (MyScrollRect.vertical)
        {
            nPos.y = Mathf.Clamp01((pos.y + cellSize.y / 2f - MyScrollRect.viewport.rect.height / 2f) / (rectTransform.rect.height - MyScrollRect.viewport.rect.height));
            if (startCorner == Corner.UpperLeft || startCorner == Corner.UpperRight)
                nPos.y = 1 - nPos.y;
        }
        if (MyScrollRect.horizontal)
        {
            nPos.x = Mathf.Clamp01((pos.x + cellSize.x / 2f - MyScrollRect.viewport.rect.width / 2f) / (rectTransform.rect.width - MyScrollRect.viewport.rect.width));
            if (startCorner == Corner.LowerRight || startCorner == Corner.UpperRight)
                nPos.x = 1 - nPos.x;
        }
        return nPos;
    }

    public ScrollRect MyScrollRect
    {
        get
        {
            if(myScrollRect == null)
            {
                Transform trans = transform;
                while(trans != null)
                {
                    myScrollRect = trans.GetComponent<ScrollRect>();
                    if(myScrollRect != null && myScrollRect.content == rectTransform)
                    {
                        break;
                    }
                    else
                    {
                        trans = trans.parent;
                    }
                }
                if (myScrollRect != null)
                {
                    myScrollRect.onValueChanged.AddListener(OnScroll);
                }
            }
            return myScrollRect;
        }
    }

    #region Layout

    public override void CalculateLayoutInputHorizontal()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            base.CalculateLayoutInputHorizontal();
            return;
        }
#endif

        int minColumns = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minColumns = m_ConstraintCount;
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minColumns  = Mathf.CeilToInt(MyCount / (float)m_ConstraintCount - 0.001f);
        }
        else
        {
            minColumns = Mathf.Max(1, Mathf.FloorToInt((rectTransform.sizeDelta.x - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            //如果以垂直方向开始布局，则以高度为准调整宽度
            if (startAxis == Axis.Vertical)
            {
                float hight = rectTransform.rect.size.y;
                int cellCountY = Mathf.Max(1, Mathf.FloorToInt((hight - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
                minColumns = Mathf.CeilToInt(MyCount / (float)cellCountY);
            }
        }
        float minWidth = padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x;
        SetLayoutInputForAxis(minWidth,minWidth,-1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            base.CalculateLayoutInputVertical();
            return;
        }
#endif

        int minRows = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minRows = Mathf.CeilToInt(MyCount / (float)m_ConstraintCount - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minRows = m_ConstraintCount;
        }
        else
        {
            minRows = Mathf.Max(1, Mathf.FloorToInt((rectTransform.sizeDelta.y - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
            //如果以水平方向开始布局，则以宽度为准调整高度
            if (startAxis == Axis.Horizontal)
            {
                float width = rectTransform.rect.size.x;
                int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
                minRows = Mathf.CeilToInt(MyCount / (float)cellCountX);
            }

        }

        float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
        SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            base.SetLayoutHorizontal();
            return;
        }
#endif

        SetPreferredWidth();
    }

    public override void SetLayoutVertical()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            base.SetLayoutVertical();
            return;
        }
#endif
        
        SetPrefferedHeight();
    }

    #endregion

    private void CalculateCellsPosition()
    {
        myCellsPosition.Clear();

        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        int cellCountX = 1;
        int cellCountY = 1;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            cellCountX = m_ConstraintCount;
            cellCountY = Mathf.CeilToInt(MyCount / (float)cellCountX - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            cellCountY = m_ConstraintCount;
            cellCountX = Mathf.CeilToInt(MyCount / (float)cellCountY - 0.001f);
        }
        else
        {
            if (cellSize.x + spacing.x <= 0)
                cellCountX = int.MaxValue;
            else
                cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

            if (cellSize.y + spacing.y <= 0)
                cellCountY = int.MaxValue;
            else
                cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
        }

        int cornerX = (int)startCorner % 2;
        int cornerY = (int)startCorner / 2;

        int cellsPerMainAxis, actualCellCountX, actualCellCountY;
        if (startAxis == Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = Mathf.Clamp(cellCountX, 1, MyCount);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(MyCount / (float)cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = Mathf.Clamp(cellCountY, 1, MyCount);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(MyCount / (float)cellsPerMainAxis));
        }

        Vector2 requiredSpace = new Vector2(
                actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
                actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
                );
        Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
                );

        for (int i = 0; i < MyCount; i++)
        {
            int positionX;
            int positionY;
            if (startAxis == Axis.Horizontal)
            {
                positionX = i % cellsPerMainAxis;
                positionY = i / cellsPerMainAxis;
            }
            else
            {
                positionX = i / cellsPerMainAxis;
                positionY = i % cellsPerMainAxis;
            }

            if (cornerX == 1)
                positionX = actualCellCountX - 1 - positionX;
            if (cornerY == 1)
                positionY = actualCellCountY - 1 - positionY;

            //原点在左上
            myCellsPosition.Add(new Vector2(startOffset.x + (cellSize[0] + spacing[0]) * positionX,
                startOffset.y + (cellSize[1] + spacing[1]) * positionY));
            //Debug.LogError(cellsPosition.Last());
        }
    }

    public void Init(GameObject itemPrefab,int count)
    {
        Init(itemPrefab);
        MyCount = count;
    }

    public void Init(GameObject itemPrefab)
    {
        if (myItemPool == null)
        {
            myItemPool = new ScrollItemPool(itemPrefab, transform);
        }
        else
        {
            if (itemPrefab != myItemPool.MyItemPrefab)
            {
                Clear();
            }
            myItemPool.MyItemPrefab = itemPrefab;
        }
    }

    //public void Init(int count,Func<int,RectTransform> OnGetItem,Action<int,RectTransform> OnCacheItem)
    //{
    //    this.OnGetItem += OnGetItem;
    //    this.OnCacheItem += OnCacheItem;
    //    MyCount = count;
    //}

    public void Clear()
    {
        foreach (var visibleItem in myVisibleItems)
        {
            CacheItem(visibleItem.Key, visibleItem.Value);
        }
        myVisibleItems.Clear();
        myCount = 0;
        myCellsPosition.Clear();
    }

    public void RefreshGrid()
    {
        if (MyScrollRect != null)
        {
            UpdateVisibleItems(MyScrollRect.normalizedPosition, true);
        }
    }

    void UpdateVisibleItems(Vector2 scrollPos,bool clear)
    {
        //clamp01
        //scrollPos = Vector2.Max(Vector2.zero, scrollPos);
        //scrollPos = Vector2.Min(Vector2.one, scrollPos);

        //Debug.Log("{" + scrollPos.x + "," + scrollPos.y + "}");

        myVisibleItemIndex.Clear();

        Vector2 contentSize = rectTransform.rect.size;
        Vector2 viewSize = myScrollRect.viewport.rect.size;
        if (rectTransform != myScrollRect.viewport && rectTransform.parent == myScrollRect.viewport)
        {
            viewSize.x /= rectTransform.localScale.x;
            viewSize.y /= rectTransform.localScale.y;
        }
        Vector2 visiblePaddingSize = new Vector2(myVisiblePadding.horizontal, myVisiblePadding.vertical);
        Vector2 adjustedViewSize = viewSize + visiblePaddingSize;

        contentSize = Vector2.Max(viewSize, contentSize);

        //原点在左下
        Vector2 visibleLeftBottom = Vector2.Scale((contentSize - viewSize), scrollPos) - new Vector2(myVisiblePadding.left,myVisiblePadding.bottom);
        Vector2 visibleRightTop = visibleLeftBottom + adjustedViewSize;

        //原点改为左上
        visibleLeftBottom.y = contentSize.y - visibleLeftBottom.y;
        visibleRightTop.y = contentSize.y - visibleRightTop.y;

        for(int i = 0;i < MyCount;i++)
        {
            bool visible = true;
            if(myScrollRect.vertical)
            {
                if(myCellsPosition[i].y > visibleLeftBottom.y || myCellsPosition[i].y + cellSize.y < visibleRightTop.y)
                {
                    visible = false;
                }
            }

            if(visible && MyScrollRect.horizontal)
            {
                if(myCellsPosition[i].x > visibleRightTop.x || myCellsPosition[i].x + cellSize.x < visibleLeftBottom.x)
                {
                    visible = false;
                }
            }

            if(visible)
            {
                myVisibleItemIndex.Add(i);
            }
        }

        myCacheVisibleItems.Clear();

        if (!clear)
        {
            for(int i = myVisibleItemIndex.Count-1;i>= 0;i--)
            {
                int index = myVisibleItemIndex[i];
                if(myVisibleItems.ContainsKey(index))
                {
                    myCacheVisibleItems.Add(index, myVisibleItems[index]);
                    myVisibleItems.Remove(index);
                    myVisibleItemIndex.RemoveAt(i);
                }
            }
        }

        bool removeItem = false;
        foreach (var item in myVisibleItems)
        {
            removeItem = true;
            CacheItem(item.Key, item.Value);
        }
        myVisibleItems.Clear();

        foreach(var item in myCacheVisibleItems)
        {
            myVisibleItems.Add(item.Key, item.Value);
        }

        if(myVisibleItemIndex.Count == 0)
        {
            if(removeItem && OnVisibleItemUpdate != null)
                OnVisibleItemUpdate();
            return;
        }

        foreach(int i in myVisibleItemIndex)
        {
            myVisibleItems.Add(i, GetItem(i));
        }

        foreach (var item in myVisibleItems)
        {
            RectTransform rect = item.Value;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                DrivenTransformProperties.AnchoredPosition |
                DrivenTransformProperties.SizeDelta);

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.sizeDelta = cellSize;

            Vector2 pos = myCellsPosition[item.Key];
            SetChildAlongAxis(rect, 0, pos[0], cellSize[0]);
            SetChildAlongAxis(rect, 1, pos[1], cellSize[1]);
        }

        if (OnVisibleItemUpdate != null)
            OnVisibleItemUpdate();
    }

    void OnScroll(Vector2 normalizedPosition)
    {
        UpdateVisibleItems(normalizedPosition, false);
    }

    void CacheItem(int index,RectTransform item)
    {
        if(OnCacheItem != null)
        {
            Debug.Assert(OnGetItem != null, "if you set OnCacheItem, you must also set OnGetItem");
            OnCacheItem(index, item);
            return;
        }

        myItemPool.CacheItem(item);

        if (OnAfterCacheItem != null)
            OnAfterCacheItem(index, item);
    }

    RectTransform GetItem(int i)
    {
        if(OnGetItem != null)
        {
            Debug.Assert(OnCacheItem != null, "if you set OnGetItem, you must also set OnCacheItem");
            return OnGetItem(i);
        }

        RectTransform rt = myItemPool.GetItem();

        try
        {
            if (OnFillItem != null)
            {
                OnFillItem(i, rt);
            }
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }

        return rt;
    }

    protected override void OnTransformChildrenChanged()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            base.OnTransformChildrenChanged();
        }
#endif
    }


    public bool IsItemVisible(int itemIndex)
    {
        return myVisibleItems.ContainsKey(itemIndex);
    }

    public RectTransform GetVisibleItem(int itemIndex)
    {
        if(myVisibleItems.ContainsKey(itemIndex))
        {
            return myVisibleItems[itemIndex];
        }
        return null;
    }

    public Dictionary<int, RectTransform> GetVisibleItems()
    {
        return myVisibleItems;
    }


#if UNITY_EDITOR
    protected override void OnValidate()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        CalculateCellsPosition();
        RefreshGrid();
    }

#endif
}
