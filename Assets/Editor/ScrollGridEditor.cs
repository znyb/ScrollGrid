using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ScrollGrid))]
public class ScrollGridEditor : GridLayoutGroupEditor 
{
    SerializedProperty myViewPadding;

    protected override void OnEnable()
    {
        base.OnEnable();
        myViewPadding = serializedObject.FindProperty("myVisiblePadding");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(myViewPadding, true);
        serializedObject.ApplyModifiedProperties();
    }

}
