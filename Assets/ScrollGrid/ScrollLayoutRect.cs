using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class ScrollLayoutRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
{
    public enum Axis
    {
        Horizontal = 0,
        Vertical = 1
    }

    public RectTransform Content;

    public RectTransform Viewport;

    public Axis ScrollAxis = Axis.Vertical;

    public float Elasticity = 0.1f; 
    
    public bool Inertia = true;
    
    public float DecelerationRate = 0.135f; // Only used when inertia is enabled
    
    public float ScrollSensitivity = 1.0f;
    
    // The offset from handle position to mouse down position
    private Vector2 m_PointerStartLocalCursor = Vector2.zero;
    protected Vector2 m_ContentStartPosition = Vector2.zero;

    private RectTransform m_ViewRect;

    protected RectTransform viewRect
    {
        get
        {
            if (m_ViewRect == null)
                m_ViewRect = Viewport;
            if (m_ViewRect == null)
                m_ViewRect = (RectTransform)transform;
            return m_ViewRect;
        }
    }

    protected Bounds m_ContentBounds;
    private Bounds m_ViewBounds;

    private Vector2 m_Velocity;
    public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

    private bool m_Dragging;
    public bool Dragging { get { return m_Dragging; } }

    private Vector2 m_PrevPosition = Vector2.zero;
    private Bounds m_PrevContentBounds;
    private Bounds m_PrevViewBounds;
    [NonSerialized]
    private bool m_HasRebuiltLayout = false;

    [NonSerialized] private RectTransform m_Rect;
    private RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    public virtual void Rebuild(CanvasUpdate executing)
    {
        if (executing == CanvasUpdate.PostLayout)
        {
            UpdateBounds();
            UpdatePrevData();

            m_HasRebuiltLayout = true;
        }
    }

    public virtual void LayoutComplete()
    { }

    public virtual void GraphicUpdateComplete()
    { }

    protected override void OnEnable()
    {
        base.OnEnable();

        CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
    }

    protected override void OnDisable()
    {
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

        m_HasRebuiltLayout = false;
        m_Velocity = Vector2.zero;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    public override bool IsActive()
    {
        return base.IsActive() && Content != null;
    }

    private void EnsureLayoutHasRebuilt()
    {
        if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
            Canvas.ForceUpdateCanvases();
    }

    public virtual void StopMovement()
    {
        m_Velocity = Vector2.zero;
    }

    public virtual void OnScroll(PointerEventData data)
    {
        if (!IsActive())
            return;

        EnsureLayoutHasRebuilt();
        UpdateBounds();

        Vector2 delta = data.scrollDelta;
        // Down is positive for scroll events, while in UI system up is positive.
        delta.y *= -1;
        if (ScrollAxis == Axis.Vertical)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                delta.y = delta.x;
            delta.x = 0;
        }
        else
        {
            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                delta.x = delta.y;
            delta.y = 0;
        }

        Vector2 position = Content.anchoredPosition;
        position += delta * ScrollSensitivity;

        SetContentAnchoredPosition(position);
        UpdateBounds();
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        m_Velocity = Vector2.zero;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive())
            return;

        UpdateBounds();

        m_PointerStartLocalCursor = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
        m_ContentStartPosition = Content.anchoredPosition;
        m_Dragging = true;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        m_Dragging = false;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive())
            return;

        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out localCursor))
            return;

        UpdateBounds();

        var pointerDelta = localCursor - m_PointerStartLocalCursor;
        Vector2 position = m_ContentStartPosition + pointerDelta;

        // Offset to get content into place in the view.
        Vector2 offset = CalculateOffset(position - Content.anchoredPosition);
        position += offset;

        if (offset.x != 0)
            position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
        if (offset.y != 0)
            position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);

        SetContentAnchoredPosition(position);
    }

    protected virtual void SetContentAnchoredPosition(Vector2 position)
    {
        if (ScrollAxis == Axis.Vertical)
            position.x = Content.anchoredPosition.x;
        else
            position.y = Content.anchoredPosition.y;

        if (position != Content.anchoredPosition)
        {
            Content.anchoredPosition = position;
            UpdateBounds();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!Content)
            return;

        EnsureLayoutHasRebuilt();
        UpdateBounds();
        float deltaTime = Time.unscaledDeltaTime;
        Vector2 offset = CalculateOffset(Vector2.zero);
        if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
        {
            Vector2 position = Content.anchoredPosition;
            for (int axis = 0; axis < 2; axis++)
            {
                // Apply spring physics if movement is elastic and content has an offset from the view.
                if (offset[axis] != 0)
                {
                    float speed = m_Velocity[axis];
                    position[axis] = Mathf.SmoothDamp(Content.anchoredPosition[axis], Content.anchoredPosition[axis] + offset[axis], ref speed, Elasticity, Mathf.Infinity, deltaTime);
                    if (Mathf.Abs(speed) < 1)
                        speed = 0;
                    m_Velocity[axis] = speed;
                }
                // Else move content according to velocity with deceleration applied.
                else if (Inertia)
                {
                    m_Velocity[axis] *= Mathf.Pow(DecelerationRate, deltaTime);
                    if (Mathf.Abs(m_Velocity[axis]) < 1)
                        m_Velocity[axis] = 0;
                    position[axis] += m_Velocity[axis] * deltaTime;
                }
                // If we have neither elaticity or friction, there shouldn't be any velocity.
                else
                {
                    m_Velocity[axis] = 0;
                }
            }

            SetContentAnchoredPosition(position);
        }

        if (m_Dragging && Inertia)
        {
            Vector3 newVelocity = (Content.anchoredPosition - m_PrevPosition) / deltaTime;
            m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
        }

        if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || Content.anchoredPosition != m_PrevPosition)
        {
            UpdatePrevData();
        }
    }

    protected void UpdatePrevData()
    {
        if (Content == null)
            m_PrevPosition = Vector2.zero;
        else
            m_PrevPosition = Content.anchoredPosition;
        m_PrevViewBounds = m_ViewBounds;
        m_PrevContentBounds = m_ContentBounds;
    }


    private static float RubberDelta(float overStretching, float viewSize)
    {
        return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    public virtual void CalculateLayoutInputHorizontal() { }
    public virtual void CalculateLayoutInputVertical() { }

    public virtual float minWidth { get { return -1; } }
    public virtual float preferredWidth { get { return -1; } }
    public virtual float flexibleWidth { get { return -1; } }

    public virtual float minHeight { get { return -1; } }
    public virtual float preferredHeight { get { return -1; } }
    public virtual float flexibleHeight { get { return -1; } }

    public virtual int layoutPriority { get { return -1; } }

    public virtual void SetLayoutHorizontal()
    {
        
    }

    public virtual void SetLayoutVertical()
    {
        m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
        m_ContentBounds = GetBounds();
    }


    protected void UpdateBounds()
    {
        m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
        m_ContentBounds = GetBounds();

        if (Content == null)
            return;

        Vector3 contentSize = m_ContentBounds.size;
        Vector3 contentPos = m_ContentBounds.center;
        var contentPivot = Content.pivot;
        AdjustBounds(ref m_ViewBounds, ref contentPivot, ref contentSize, ref contentPos);
        m_ContentBounds.size = contentSize;
        m_ContentBounds.center = contentPos;
    }

    internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
    {
        // Make sure content bounds are at least as large as view by adding padding if not.
        // One might think at first that if the content is smaller than the view, scrolling should be allowed.
        // However, that's not how scroll views normally work.
        // Scrolling is *only* possible when content is *larger* than view.
        // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
        // E.g. if pivot is at top, bounds are expanded downwards.
        // This also works nicely when ContentSizeFitter is used on the content.
        Vector3 excess = viewBounds.size - contentSize;
        if (excess.x > 0)
        {
            contentPos.x -= excess.x * (contentPivot.x - 0.5f);
            contentSize.x = viewBounds.size.x;
        }
        if (excess.y > 0)
        {
            contentPos.y -= excess.y * (contentPivot.y - 0.5f);
            contentSize.y = viewBounds.size.y;
        }
    }

    private readonly Vector3[] m_Corners = new Vector3[4];
    private Bounds GetBounds()
    {
        if (Content == null)
            return new Bounds();
        Content.GetWorldCorners(m_Corners);
        var viewWorldToLocalMatrix = viewRect.worldToLocalMatrix;
        return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
    }

    internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
    {
        var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int j = 0; j < 4; j++)
        {
            Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
            vMin = Vector3.Min(v, vMin);
            vMax = Vector3.Max(v, vMax);
        }

        var bounds = new Bounds(vMin, Vector3.zero);
        bounds.Encapsulate(vMax);
        return bounds;
    }

    private Vector2 CalculateOffset(Vector2 delta)
    {
        return InternalCalculateOffset(ref m_ViewBounds, ref m_ContentBounds, ScrollAxis, ref delta);
    }

    internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, Axis axis, ref Vector2 delta)
    {
        Vector2 offset = Vector2.zero;

        Vector2 min = contentBounds.min;
        Vector2 max = contentBounds.max;

        if (axis == Axis.Horizontal)
        {
            min.x += delta.x;
            max.x += delta.x;
            if (min.x > viewBounds.min.x)
                offset.x = viewBounds.min.x - min.x;
            else if (max.x < viewBounds.max.x)
                offset.x = viewBounds.max.x - max.x;
        }
        else
        {
            min.y += delta.y;
            max.y += delta.y;
            if (max.y < viewBounds.max.y)
                offset.y = viewBounds.max.y - max.y;
            else if (min.y > viewBounds.min.y)
                offset.y = viewBounds.min.y - min.y;
        }

        return offset;
    }

    protected void SetDirty()
    {
        if (!IsActive())
            return;

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    protected void SetDirtyCaching()
    {
        if (!IsActive())
            return;

        CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirtyCaching();
    }

#endif




    public RectOffset myPadding = new RectOffset();
    public Vector2 mySpacing;
    public RectOffset myVisiblePadding = new RectOffset();

    public event Func<int,RectTransform> OnGetItem;
    public event Action<int, RectTransform> OnCacheItem;

    public event Action OnVisibleItemUpdate;

    Dictionary<int, RectTransform> myVisibleItems = new Dictionary<int, RectTransform>();

    List<int> myVisibleItemIndex = new List<int>();
    Dictionary<int, RectTransform> myCacheVisibleItems = new Dictionary<int, RectTransform>();

    public void Clear()
    {
        foreach (var visibleItem in myVisibleItems)
        {
            CacheItem(visibleItem.Key, visibleItem.Value);
        }
        myVisibleItems.Clear();
    }

    // 左下，左上，右上，右下
    Vector3[] ViewCorners = new Vector3[4];
    Vector3[] ItemCorners = new Vector3[4];

    void GetViewCorners()
    {
        viewRect.GetWorldCorners(ViewCorners);
        ViewCorners[0] -= new Vector3(myVisiblePadding.left, myVisiblePadding.bottom);
        ViewCorners[1] += new Vector3(-myVisiblePadding.left, myVisiblePadding.top);
        ViewCorners[2] += new Vector3(myVisiblePadding.right, myVisiblePadding.top);
        ViewCorners[3] += new Vector3(myVisiblePadding.right, -myVisiblePadding.bottom);
    }
    void ScrollUp()
    {
        myVisibleItemIndex.Sort();
        GetViewCorners();
        foreach(var index in myVisibleItemIndex)
        {
            myVisibleItems[index].GetWorldCorners(ItemCorners);
            if (ItemCorners[0][(int)ScrollAxis] > ViewCorners[0][(int)ScrollAxis])
            {

            }
            else break;
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
        Vector2 viewSize = Viewport.rect.size;
        Vector2 visiblePaddingSize = new Vector2(myVisiblePadding.horizontal, myVisiblePadding.vertical);
        Vector2 adjustedViewSize = viewSize + visiblePaddingSize;

        contentSize = Vector2.Max(viewSize, contentSize);

        //原点在左下
        Vector2 visibleLeftBottom = Vector2.Scale((contentSize - viewSize), scrollPos) - new Vector2(myVisiblePadding.left,myVisiblePadding.bottom);
        Vector2 visibleRightTop = visibleLeftBottom + adjustedViewSize;

        //原点改为左上
        visibleLeftBottom.y = contentSize.y - visibleLeftBottom.y;
        visibleRightTop.y = contentSize.y - visibleRightTop.y;


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

        if (OnVisibleItemUpdate != null)
            OnVisibleItemUpdate();
    }

    void CacheItem(int index,RectTransform item)
    {
        if(OnCacheItem != null)
        {
            OnCacheItem(index, item);
            return;
        }
    }

    RectTransform GetItem(int i)
    {
        if(OnGetItem != null)
        {
            return OnGetItem(i);
        }
        
        return null;
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

}
