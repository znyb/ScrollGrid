using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PreferredSizeClamp : UIBehaviour,ILayoutElement
{
    public Vector2 myHorizontalClamp;
    public Vector2 myVerticalClamp;
    public int myLayoutPriority = 101;

    ILayoutElement _LayoutElement;

    ILayoutElement LayoutElement
    {
        get
        {
            if(_LayoutElement == null)
            {
                var elements = GetComponents<ILayoutElement>();
                int priority = -1;
                foreach (var e in elements)
                {
                    if ((object)e == this)
                        continue;
                    if(e.layoutPriority > priority)
                    {
                        priority = e.layoutPriority;
                        _LayoutElement = e;
                    }
                }
                if (_LayoutElement == null)
                    Debug.LogError("gameObject do not have a ILayoutElement");
            }
            return _LayoutElement;
        }
    }

    public float minWidth
    {
        get
        {
            if (LayoutElement == null)
                return 0;
            return LayoutElement.minWidth;
        }
    }

    public float preferredWidth
    {
        get
        {
            if (LayoutElement == null)
                return 0;

            return Mathf.Clamp(LayoutElement.preferredWidth, myHorizontalClamp.x, myHorizontalClamp.y);
        }
    }

    public float flexibleWidth
    {
        get
        {
            if (LayoutElement == null)
                return -1;
            return LayoutElement.flexibleWidth;
        }
    }

    public float minHeight
    {
        get
        {
            if (LayoutElement == null)
                return 0;
            return LayoutElement.minHeight;
        }
    }

    public float preferredHeight
    {
        get
        {
            if (LayoutElement == null)
                return 0;

            return Mathf.Clamp(LayoutElement.preferredHeight, myVerticalClamp.x, myVerticalClamp.y);
        }
    }

    public float flexibleHeight
    {
        get
        {
            if (LayoutElement == null)
                return -1;
            return LayoutElement.flexibleHeight;
        }
    }

    public int layoutPriority
    {
        get
        {
            return myLayoutPriority;
        }
    }

    public void CalculateLayoutInputHorizontal()
    {
        
    }

    public void CalculateLayoutInputVertical()
    {
        
    }
}
