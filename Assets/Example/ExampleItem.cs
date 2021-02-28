using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleItem : MonoBehaviour
{
    public Text myNumText;

    public void Init(int num)
    {
        myNumText.text = num.ToString();
    }

}
