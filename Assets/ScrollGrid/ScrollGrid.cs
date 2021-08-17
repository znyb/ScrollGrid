using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ScrollGrid : ScrollGrid<RectTransform>
{
    protected override RectTransform GetRectTransform(RectTransform item)
    {
        return item;
    }
}

public interface IRectTransformProvider
{
    RectTransform rectTransform { get; }
}

public class ScrollGrid<T> : ScrollGridBase where T : Component
{
    public event Action<int, T> OnFillItem;

    public event Func<int,T> OnGetItem;
    public event Action<int, T> OnCacheItem;

    public event Action OnVisibleItemUpdate;

    public bool myUseRectTransformCache = true;

    ItemPool<T> myItemPool;

    Dictionary<int, T> myVisibleItems = new Dictionary<int, T>();
    Dictionary<int, T> myCacheVisibleItems = new Dictionary<int, T>();

    ItemPool<T> ItemPool
    {
        get
        {
            if(myItemPool == null)
            {
                myItemPool = new ItemPool<T>();
                myItemPool.myItemPrefab = myItemPrefab;
                myItemPool.myCacheType = myItemCacheType;
                myItemPool.myParent = transform;
            }
            return myItemPool;
        }
    }

    public void Clear()
    {
        foreach (var visibleItem in myVisibleItems)
        {
            CacheItem(visibleItem.Key, visibleItem.Value);
        }
        myVisibleItems.Clear();
        myCount = 0;
        //myCellsPosition.Clear();
    }

    protected override void UpdateVisibleItems(bool clear)
    {
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
            RectTransform rect = GetRectTransform(item.Value);

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

    Dictionary<T, RectTransform> rectTransformCache;
    protected virtual RectTransform GetRectTransform(T item)
    {
        if (item is IRectTransformProvider provider)
            return provider.rectTransform;

        if (myUseRectTransformCache)
        {
            if (rectTransformCache == null)
                rectTransformCache = new Dictionary<T, RectTransform>();
            if (!rectTransformCache.TryGetValue(item, out var rect))
            {
                rect = item.transform as RectTransform;
                if (rect == null)
                {
                    Debug.LogError("item do not has RectTransform");
                }
                rectTransformCache[item] = rect;
            }
            return rect;
        }
        else
            return item.transform as RectTransform;
    }

    void CacheItem(int index,T item)
    {
        if(OnCacheItem != null)
        {
            Debug.Assert(OnGetItem != null, "if you set OnCacheItem, you must also set OnGetItem");
            OnCacheItem(index, item);
            return;
        }

        ItemPool.CacheItem(item);
    }

    T GetItem(int i)
    {
        if(OnGetItem != null)
        {
            Debug.Assert(OnCacheItem != null, "if you set OnGetItem, you must also set OnCacheItem");
            return OnGetItem(i);
        }

        T rt = ItemPool.GetItem();

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

    public bool IsItemVisible(int itemIndex)
    {
        return myVisibleItems.ContainsKey(itemIndex);
    }

    public T GetVisibleItem(int itemIndex)
    {
        if(myVisibleItems.ContainsKey(itemIndex))
        {
            return myVisibleItems[itemIndex];
        }
        return null;
    }

    public Dictionary<int, T> GetVisibleItems()
    {
        return myVisibleItems;
    }

}
