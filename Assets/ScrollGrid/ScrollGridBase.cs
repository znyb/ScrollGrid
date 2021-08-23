using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ScrollGridBase : ContentSizeFitGrid
{
    public GameObject myItemPrefab;
    public ItemCacheType myItemCacheType;
    public RectOffset myVisiblePadding = new RectOffset();

    public bool myPreCalcCellPosition = false;

    protected int myCount;
    protected ScrollRect myScrollRect;

    protected Vector2[] myCellsPosition;
    protected List<int> myVisibleItemIndex = new List<int>();

    public Vector2 Size
    {
        get
        {
            return rectTransform.rect.size;
        }
    }

    public int Count
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
                var nPos = ScrollRect.normalizedPosition;

                myCount = value;
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                CalculateCellsPosition();

                // 调整normalizedPosition，确保前面的物件位置不变
                if (ScrollRect.vertical)
                {
                    if (startCorner == Corner.UpperLeft || startCorner == Corner.UpperRight)
                    {
                        var realPos = (1 - nPos.y) * preSize.y;
                        ScrollRect.verticalNormalizedPosition = 1 - realPos / rectTransform.sizeDelta.y;
                    }
                    else
                    {
                        var realPos = nPos.y * preSize.y;
                        ScrollRect.verticalNormalizedPosition = realPos / rectTransform.sizeDelta.y;
                    }
                }
                if (myScrollRect.horizontal)
                {
                    if (startCorner == Corner.LowerLeft || startCorner == Corner.UpperLeft)
                    {
                        var realPos = nPos.x * preSize.x;
                        ScrollRect.horizontalNormalizedPosition = realPos / rectTransform.sizeDelta.x;
                    }
                    else
                    {
                        var realPos = (1 - nPos.x) * preSize.x;
                        ScrollRect.horizontalNormalizedPosition = 1 - realPos / rectTransform.sizeDelta.x;
                    }
                }
            }

            //可能个数没变，但是内容变了，强制刷新
            RefreshGrid();
        }
    }

    public ScrollRect ScrollRect
    {
        get
        {
            if (myScrollRect == null)
            {
                Transform trans = transform;
                while (trans != null)
                {
                    myScrollRect = trans.GetComponent<ScrollRect>();
                    if (myScrollRect != null && myScrollRect.content == rectTransform)
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

    /// <summary>
    /// 将指定index的对象放到可见物件中间
    /// </summary>
    public void CenterItem(int index)
    {
        var pos = CalcItemNormalizedPos(index);
        ScrollRect.normalizedPosition = pos;

        //RefreshGrid();
    }

    public Vector2 CalcItemNormalizedPos(int index)
    {
        index = Mathf.Clamp(index, 0, myCount - 1);
        var pos = GetCellsPosition(index);
        Vector2 nPos = new Vector2();
        if (ScrollRect.vertical)
        {
            nPos.y = Mathf.Clamp01((pos.y + cellSize.y / 2f - ScrollRect.viewport.rect.height / 2f) / (rectTransform.rect.height - ScrollRect.viewport.rect.height));
            if (startCorner == Corner.UpperLeft || startCorner == Corner.UpperRight)
                nPos.y = 1 - nPos.y;
        }
        if (ScrollRect.horizontal)
        {
            nPos.x = Mathf.Clamp01((pos.x + cellSize.x / 2f - ScrollRect.viewport.rect.width / 2f) / (rectTransform.rect.width - ScrollRect.viewport.rect.width));
            if (startCorner == Corner.LowerRight || startCorner == Corner.UpperRight)
                nPos.x = 1 - nPos.x;
        }
        return nPos;
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
            minColumns  = Mathf.CeilToInt(Count / (float)m_ConstraintCount - 0.001f);
        }
        else
        {
            minColumns = Mathf.Max(1, Mathf.FloorToInt((rectTransform.sizeDelta.x - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            //如果以垂直方向开始布局，则以高度为准调整宽度
            if (startAxis == Axis.Vertical)
            {
                float hight = rectTransform.rect.size.y;
                int cellCountY = Mathf.Max(1, Mathf.FloorToInt((hight - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
                minColumns = Mathf.CeilToInt(Count / (float)cellCountY);
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
            minRows = Mathf.CeilToInt(Count / (float)m_ConstraintCount - 0.001f);
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
                minRows = Mathf.CeilToInt(Count / (float)cellCountX);
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


    int cellCountX = 1;
    int cellCountY = 1;
    int cellsPerMainAxis, actualCellCountX, actualCellCountY;
    float startOffsetX, startOffsetY;
    private void CalculateCellsPosition()
    {

        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        if (m_Constraint == Constraint.FixedColumnCount)
        {
            cellCountX = m_ConstraintCount;
            cellCountY = Mathf.CeilToInt(Count / (float)cellCountX - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            cellCountY = m_ConstraintCount;
            cellCountX = Mathf.CeilToInt(Count / (float)cellCountY - 0.001f);
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

        
        if (startAxis == Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Count);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(Count / (float)cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Count);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(Count / (float)cellsPerMainAxis));
        }

        Vector2 requiredSpace = new Vector2(
                actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
                actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
                );

        startOffsetX = GetStartOffset(0, requiredSpace.x);
        startOffsetY = GetStartOffset(1, requiredSpace.y);

        if (myPreCalcCellPosition)
        {
            if (myCellsPosition == null || myCellsPosition.Length < myCount)
                myCellsPosition = new Vector2[myCount];

            for (int i = 0; i < myCount; i++)
            {
                int positionX;
                int positionY;
                if (m_StartAxis == Axis.Horizontal)
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
                myCellsPosition[i] = new Vector2(startOffsetX + (m_CellSize.x + m_Spacing.x) * positionX,
                    startOffsetY + (m_CellSize.y + m_Spacing.y) * positionY);
                //Debug.LogError(cellsPosition.Last());
            }
        }
    }

    public Vector2 GetCellsPosition(int index)
    {
        if(myPreCalcCellPosition)
        {
            return myCellsPosition[index];
        }
        else
        {
            int positionX;
            int positionY;
            if (m_StartAxis == Axis.Horizontal)
            {
                positionX = index % cellsPerMainAxis;
                positionY = index / cellsPerMainAxis;
            }
            else
            {
                positionX = index / cellsPerMainAxis;
                positionY = index % cellsPerMainAxis;
            }

            if ((int)startCorner % 2 == 1)
                positionX = actualCellCountX - 1 - positionX;
            if ((int)startCorner / 2 == 1)
                positionY = actualCellCountY - 1 - positionY;

            return new Vector2(startOffsetX + (m_CellSize.x + m_Spacing.x) * positionX,
                startOffsetY + (m_CellSize.y + m_Spacing.y) * positionY);
        }
    }

    public void RefreshGrid()
    {
        if (ScrollRect != null)
        {
            UpdateVisibleItems(ScrollRect.normalizedPosition, true);
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

        int hStart = Mathf.Clamp(Mathf.FloorToInt((visibleLeftBottom.x - m_Padding.left+ m_Spacing.x) / (m_CellSize.x + m_Spacing.x)),0,cellCountX);
        int hEnd = Mathf.Clamp(Mathf.CeilToInt((visibleRightTop.x - m_Padding.left + m_Spacing.x) / (m_CellSize.x + m_Spacing.x)), 1, cellCountX);

        int vStart = Mathf.Clamp(Mathf.FloorToInt((visibleRightTop.y - m_Padding.top + m_Spacing.y) / (m_CellSize.y + m_Spacing.y)),0,cellCountY);
        int vEnd = Mathf.Clamp(Mathf.CeilToInt((visibleLeftBottom.y - m_Padding.top + m_Spacing.y) / (m_CellSize.y + m_Spacing.y)),1,cellCountY);

        //Debug.Log($"scrollPos:{scrollPos.x},{scrollPos.y},cellCountX:{cellCountX},cellCountY:{cellCountY},hStart:{hStart},hEnd:{hEnd},vStart:{vStart},vEnd:{vEnd}");

        if ((int)startCorner % 2 == 1)
        {
            int temp = hStart;
            hStart = cellCountX - hEnd;
            hEnd = cellCountX - temp;
        }

        if ((int)startCorner / 2 == 1)
        {
            int temp = vStart;
            vStart = cellCountY - vEnd;
            vEnd = cellCountY - temp;
        }

        if (m_StartAxis == Axis.Vertical)
        {
            int temp = hStart;
            hStart = vStart;
            vStart = temp;

            temp = hEnd;
            hEnd = vEnd;
            vEnd = temp;
        }

        vEnd--;
        // 一个cellCountX行cellCountY列的表格
        // 获取到其中第hStart行第vStart列和hEnd行vEnd列所包围的单元格索引
        int end = vEnd * cellsPerMainAxis + hEnd;
        if (myCount < end)
            end = myCount;
        int startCount = vStart * cellsPerMainAxis;
        hStart += startCount;
        hEnd += startCount;
        for (; hStart < end; hStart += cellsPerMainAxis, hEnd += cellsPerMainAxis)
        {
            for (int i = hStart; i < hEnd && i < end; i++)
                myVisibleItemIndex.Add(i);
        }

        UpdateVisibleItems(clear);
    }

    protected virtual void UpdateVisibleItems(bool clear)
    {

    }

    void OnScroll(Vector2 normalizedPosition)
    {
        UpdateVisibleItems(normalizedPosition, false);
    }

#if UNITY_EDITOR
    protected override void OnTransformChildrenChanged()
    {
        if(!Application.isPlaying)
        {
            base.OnTransformChildrenChanged();
        }
    }

    protected override void OnValidate()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        CalculateCellsPosition();
        RefreshGrid();
    }

#endif
}
