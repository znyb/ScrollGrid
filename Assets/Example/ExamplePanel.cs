using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExamplePanel : MonoBehaviour
{
    public ExampleItemGrid myGrid;
    public List<int> myNums = new List<int>();

    private void Awake()
    {
        myGrid.OnFillItem += MyGrid_OnFillItem;
        for (int i = 0; i < 1000; i++)
            myNums.Add(i);
    }

    private void Start()
    {
        myGrid.Count = myNums.Count;
    }

    private void MyGrid_OnFillItem(int index, ExampleItem item)
    {
        item.Init(myNums[index]);
    }
}
