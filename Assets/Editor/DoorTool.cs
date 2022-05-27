using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

[EditorTool("Door Tool")]
public class DoorTool : EditorTool
{
    GUIContent iconContent;
    public override GUIContent toolbarIcon => iconContent;

    void OnEnable()
    {
        iconContent = new GUIContent()
        {
            image = null,
            text = "Door Tool",
            tooltip = "Door Tool",
        };
    }

    public override void OnToolGUI(EditorWindow window)
    {
        // doesn't work, can't draw gizmos here, not like this at least. it was just a test anyway
        // if (Selection.activeTransform != null)
        //     Gizmos.DrawCube(Selection.activeTransform.position, Vector3.one);
    }
}
