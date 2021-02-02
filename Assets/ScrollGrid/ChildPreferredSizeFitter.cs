using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class ChildPreferredSizeFitter : UIBehaviour, ILayoutGroup,ILayoutElement
{
    // 必须为当前节点的子节点， 子节点缩放必须为1
    public RectTransform myChild;
    public bool myHorizontalFit;
    public bool myVerticalFit;
    public Vector2 mySizePadding;
    public Vector2 myMinClamp;
    public Vector2 myMaxClamp;

    private DrivenRectTransformTracker m_Tracker;
    private RectTransform m_Rect;
    private RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    public float minWidth { get { return -1; } }

    public float preferredWidth { get { return GetPreferredSize(0); } }

    public float flexibleWidth { get { return -1; } }

    public float minHeight { get { return -1; } }

    public float preferredHeight { get { return GetPreferredSize(1); } }

    public float flexibleHeight { get { return -1; } }

    public int layoutPriority { get { return 100; } }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        m_Tracker.Clear();
        LayoutRebuilder.MarkLayoutForRebuild(myChild);
        base.OnDisable();
    }

    //protected override void OnRectTransformDimensionsChange()
    //{
    //    SetDirty();
    //}

    public virtual void SetLayoutHorizontal()
    {
        m_Tracker.Clear();
        if (myChild == null || !myHorizontalFit)
        {
            return;
        }
        m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetPreferredSize(0));
    }

    public virtual void SetLayoutVertical()
    {
        if (myChild == null || !myVerticalFit)
        {
            return;
        }
        m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GetPreferredSize(1));
    }

    float GetPreferredSize(int axis)
    {
        if (myChild == null)
            return -1;

        float size = LayoutUtility.GetPreferredSize(myChild, axis) + mySizePadding[axis];
        if (myMinClamp[axis] > 0 && size < myMinClamp[axis])
            size = myMinClamp[axis];
        if (myMaxClamp[axis] > 0 && size > myMaxClamp[axis])
            size = myMaxClamp[axis];

        return size;
    }

    protected void SetDirty()
    {
        if (!IsActive())
            return;
        if(myChild)
            LayoutRebuilder.MarkLayoutForRebuild(myChild);
        else
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    public void CalculateLayoutInputHorizontal()
    {
        
    }

    public void CalculateLayoutInputVertical()
    {
        
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
}
