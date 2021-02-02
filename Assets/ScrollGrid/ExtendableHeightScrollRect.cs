using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtendableHeightScrollRect : ScrollRect
{
    public float myExtendHeight;
    float myDefaultHeight;

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

    protected override void Start()
    {
        base.Start();
        myDefaultHeight = rectTransform.rect.size.y;
    }

    public void ResetHeight()
    {
        if(myDefaultHeight == 0f)
        {
            return;
        }
        float currentHeight = rectTransform.rect.size.y;
        float offset = currentHeight - myDefaultHeight;
        rectTransform.sizeDelta -= Vector2.up * offset;
        normalizedPosition = Vector2.up;
        velocity = Vector2.zero;
    }

    protected override void SetContentAnchoredPosition(Vector2 position)
    {
        if (!horizontal)
            position.x = content.anchoredPosition.x;
        if (!vertical)
            position.y = content.anchoredPosition.y;

        if (position != content.anchoredPosition)
        {
            //content高度小于最小view高度
            if (content.rect.size.y < myDefaultHeight)
            {
                content.anchoredPosition = position;
                UpdateBounds();
                return;
            }

            Bounds viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            float currentHeight = rectTransform.rect.size.y;
            Vector2 offset = position - content.anchoredPosition;
            float offsetY = offset.y;
            Vector2 topOffset = m_ContentBounds.max - viewBounds.max;
            float topOffsetY = topOffset.y + offsetY;
            //Debug.LogError("position.y:" + position.y + "\tcontent.anchoredPosition.y:" + content.anchoredPosition.y);
            //Debug.LogError("currentHeight:" + currentHeight + "\toffset:" + offset + "\tmyDefaultHeight:" + myDefaultHeight + "\tmyExtendHeight:" + myExtendHeight);

            //下滑，且下滑后content顶部低于当前view顶部
            if (offsetY < 0 && topOffsetY <= 0)
            {
                if (currentHeight > myDefaultHeight)
                {
                    if (currentHeight + offsetY < myDefaultHeight)
                    {
                        offsetY = myDefaultHeight - currentHeight;
                    }
                    rectTransform.sizeDelta += Vector2.up * offsetY;
                    content.anchoredPosition -= offset;
                    UpdatePrevData();
                    content.anchoredPosition += offset - topOffset;
                }
                else
                {
                    content.anchoredPosition = position;
                }
            }//上滑，且上滑后content顶部高于当前view顶部
            else if (offsetY > 0 && topOffsetY >= 0)
            {
                if (currentHeight < myDefaultHeight + myExtendHeight)
                {
                    if (currentHeight + offsetY > myDefaultHeight + myExtendHeight)
                    {
                        offsetY = myDefaultHeight + myExtendHeight - currentHeight;
                    }
                    rectTransform.sizeDelta += Vector2.up * offsetY;
                    content.anchoredPosition -= offset;
                    UpdatePrevData();
                    content.anchoredPosition += offset - topOffset;
                }
                else
                {
                    content.anchoredPosition = position;
                }
            }
            else
            {
                content.anchoredPosition = position;
            }
            UpdateBounds();
        }
    }
}
