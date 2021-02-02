using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ExtendableHeightScrollRect))]
public class ExtendableHeightScrollRectEditor : ScrollRectEditor
{
    SerializedProperty myExtendHeight;
    protected override void OnEnable()
    {
        base.OnEnable();
        myExtendHeight = serializedObject.FindProperty("myExtendHeight");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(myExtendHeight);
        serializedObject.ApplyModifiedProperties();
    }
}
