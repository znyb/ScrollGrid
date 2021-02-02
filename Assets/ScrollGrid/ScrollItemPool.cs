using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollItemPool 
{
    Transform myParent;
    GameObject myItemPrefab;
    Queue<RectTransform> myCacheItems = new Queue<RectTransform>();

    public GameObject MyItemPrefab
    {
        get
        {
            return myItemPrefab;
        }

        set
        {
            if (myItemPrefab != null && myItemPrefab != value)
            {
                Clear();
            }
            myItemPrefab = value;
        }
    }

    public ScrollItemPool(GameObject prefab,Transform parent)
    {
        myItemPrefab = prefab;
        myParent = parent;
    }

    public RectTransform GetItem()
    {
        RectTransform rt = null;
        if (myCacheItems.Count > 0)
        {
            rt = myCacheItems.Dequeue();
            //rt.localScale = Vector3.one;
            rt.gameObject.SetActive(true);
            rt.SendMessage("OnItemReuse", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            GameObject go = Object.Instantiate(myItemPrefab);
            rt = go.transform as RectTransform;
            rt.SetParent(myParent);
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            go.SetActive(true);
            rt.SendMessage("OnItemCreate", SendMessageOptions.DontRequireReceiver);
        }

        return rt;
    }

    public void CacheItem(RectTransform item)
    {
        item.SendMessage("OnItemCache", SendMessageOptions.DontRequireReceiver);
        //item.localScale = Vector3.zero;
        item.gameObject.SetActive(false);
        myCacheItems.Enqueue(item);
    }

    public void Clear(bool destroyCacheItems = true)
    {
        if(destroyCacheItems)
        {
            foreach(var item in myCacheItems)
            {
                Object.Destroy(item.gameObject);
            }
        }
        myCacheItems.Clear();
    }
}
