using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContentSizeFitGrid : GridLayoutGroup 
{

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        if (m_Constraint == Constraint.Flexible)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((rectTransform.sizeDelta.x - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            //如果以垂直方向开始布局，则以高度为准调整宽度
            if (startAxis == Axis.Vertical)
            {
                float hight = rectTransform.rect.size.y;
                int cellCountY = Mathf.Max(1, Mathf.FloorToInt((hight - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
                columns = Mathf.CeilToInt(rectChildren.Count / (float)cellCountY);
            }
            float minWidth = padding.horizontal + (cellSize.x + spacing.x) * columns - spacing.x;
            SetLayoutInputForAxis(minWidth, minWidth, -1, 0);
        }
    }

    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();

        
        if (m_Constraint == Constraint.Flexible)
        {
             int rows = Mathf.Max(1, Mathf.FloorToInt((rectTransform.sizeDelta.y - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
            //如果以水平方向开始布局，则以宽度为准调整高度
            if (startAxis == Axis.Horizontal)
            {
                float width = rectTransform.rect.size.x;
                int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
                rows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
            }
            float minSpace = padding.vertical + (cellSize.y + spacing.y) * rows - spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }
    }

    public override void SetLayoutHorizontal()
    {
        SetPreferredWidth();
        base.SetLayoutHorizontal();
    }

    public override void SetLayoutVertical()
    {
        SetPrefferedHeight();
        base.SetLayoutVertical();
    }


    protected override void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    public void SetPreferredWidth()
    {
        if (m_Constraint == Constraint.FixedRowCount || (m_Constraint == Constraint.Flexible && startAxis == Axis.Vertical))
        {
            m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LayoutUtility.GetPreferredWidth(rectTransform));
            //rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, pos, LayoutUtility.GetPreferredWidth(rectTransform));
        }
    }

    public void SetPrefferedHeight()
    {
        if (m_Constraint == Constraint.FixedColumnCount || (m_Constraint == Constraint.Flexible && startAxis == Axis.Horizontal))
        {
            m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredHeight(rectTransform));
        }
    }
}
