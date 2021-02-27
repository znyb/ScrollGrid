using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ScrollGridBase), true)]
public class ScrollGridEditor : GridLayoutGroupEditor 
{
    SerializedProperty myItemPrefab;
    SerializedProperty myViewPadding;
    GUIContent myItemPrefabLabel;
    GUIContent myViewPaddingLabel;

    protected override void OnEnable()
    {
        base.OnEnable();
        myItemPrefab = serializedObject.FindProperty("myItemPrefab");
        myItemPrefabLabel = new GUIContent("Item Prefab");
        myViewPadding = serializedObject.FindProperty("myVisiblePadding");
        myViewPaddingLabel = new GUIContent("Visible Padding");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(myItemPrefab, myItemPrefabLabel, true);
        EditorGUILayout.PropertyField(myViewPadding, myViewPaddingLabel, true);
        serializedObject.ApplyModifiedProperties();
    }

}
