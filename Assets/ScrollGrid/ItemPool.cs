using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemCacheType
{
    Active,
    Scale
}

public interface IPoolItem
{
    void OnItemCreate();
    void OnItemReuse();
    void OnItemCache();
}

public class ItemPool<T>  where T : Component
{
    public Transform myParent;
    public GameObject myItemPrefab;
    public ItemCacheType myCacheType;
    public List<T> myInitItems;
    Queue<T> myCacheItems;
    List<T> myGetItems = new List<T>();

    public List<T> AllGetItems
    {
        get
        {
            return myGetItems;
        }
    }

    void TryInit()
    {
        if (myCacheItems == null)
        {
            if(myInitItems == null)
                myCacheItems = new Queue<T>();
            else
                myCacheItems = new Queue<T>(myInitItems);
                
        }
    }

    public T GetItem()
    {
        TryInit();

        T rt = GetCacheItem();
        if (rt == null)
        {
            GameObject go = Object.Instantiate(myItemPrefab);
            rt = go.GetComponent<T>();
            if (rt == null)
                Debug.LogError("item not has Component :" + typeof(T));
            if (myParent != null)
            {
                Transform trans = rt.transform;
                trans.SetParent(myParent);
                trans.localPosition = Vector3.zero;
                trans.localRotation = Quaternion.identity;
                trans.localScale = Vector3.one;
            }
            go.SetActive(true);
            if (rt is IPoolItem itme)
                itme.OnItemCreate();
        }

        myGetItems.Add(rt);

        return rt;
    }

    T GetCacheItem()
    {
        while (myCacheItems.Count > 0)
        {
            var rt = myCacheItems.Dequeue();
            if(rt == null)
            {
                Debug.LogError("the item has been destroyed!!! you should not destroy items in the pool");
                continue;
            }
            OnGet(rt);
            if (rt is IPoolItem item)
                item.OnItemReuse();
            return rt;
        }
        return null;
    }

    public void CacheItem(T item)
    {
        TryInit();

        if (item is IPoolItem poolItem)
            poolItem.OnItemCache();
        OnCache(item);
        myGetItems.Remove(item);
        myCacheItems.Enqueue(item);
    }

    public void CacheItems(IEnumerable<T> items)
    {
        foreach(var item in items)
        {
            CacheItem(item);
        }
    }

    public void CacheAllGetItems()
    {
        TryInit();

        foreach (var item in myGetItems)
        {
            if (item is IPoolItem poolItem)
                poolItem.OnItemCache();
            OnCache(item);
            myCacheItems.Enqueue(item);
        }
        myGetItems.Clear();
    }

    void OnCache(T item)
    {
        switch(myCacheType)
        {
            case ItemCacheType.Active:
                item.gameObject.SetActive(false);
                break;
            case ItemCacheType.Scale:
                item.transform.localScale = Vector3.zero;
                break;
            default:
                Debug.LogError("myCacheType err");
                break;
        }
    }

    void OnGet(T item)
    {
        switch (myCacheType)
        {
            case ItemCacheType.Active:
                item.gameObject.SetActive(true);
                break;
            case ItemCacheType.Scale:
                item.transform.localScale = Vector3.one;
                break;
        }
    }
}
