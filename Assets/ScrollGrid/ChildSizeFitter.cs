using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChildSizeFitter : MonoBehaviour
{
    public List<RectTransform> myChilds;
    public bool myHorizontalFit;
    public bool myVerticalFit;
    public Vector2 mySizePadding;

    private Vector3[] myCorners = new Vector3[4];

    [ContextMenu("FitChildSize")]
    public void FitChildSize()
    {
        if (myChilds.Count == 0)
            return;

        if (!myHorizontalFit && !myVerticalFit)
            return;

        bool needFit = false;
        Bounds bounds = new Bounds();
        foreach(var child in myChilds)
        {
            //Debug.LogError(child.gameObject.activeInHierarchy);
            if(child.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(child);
                child.GetWorldCorners(myCorners);
                foreach(var point in myCorners)
                {
                    if(!needFit)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        needFit = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
        }
        if (!needFit)
            return;

        var rect = transform as RectTransform;
        var worldToLocalMatrix = rect.worldToLocalMatrix;
        var min = worldToLocalMatrix.MultiplyPoint3x4(bounds.min);
        var max = worldToLocalMatrix.MultiplyPoint3x4(bounds.max);
        if (myHorizontalFit)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max.x - min.x + mySizePadding.x);
        if (myVerticalFit)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, max.y - min.y + mySizePadding.y);
    }
}
