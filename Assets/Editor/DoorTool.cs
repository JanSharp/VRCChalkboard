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
    private int controlID;

    void OnEnable()
    {
        iconContent = new GUIContent()
        {
            image = null,
            text = "Door Tool",
            tooltip = "Door Tool",
        };
        controlID = GUIUtility.GetControlID(FocusType.Passive);
    }

    private Vector3? hi;

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
            return;

        var e = Event.current;
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            Vector2 mousePos = e.mousePosition;
            mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
            if (Physics.Raycast(sceneView.camera.ScreenPointToRay(mousePos), out var hit, 1000f, -1))
            {
                hi = hit.point;
            }
            e.Use();
        }
        else if (hi.HasValue && e.type == EventType.Repaint)
        {
            Handles.CubeHandleCap(0, hi.Value, Quaternion.identity, 1f, EventType.Repaint);
        }
        // doesn't work, can't draw gizmos here, not like this at least. it was just a test anyway
        // if (Selection.activeTransform != null)
        //     Gizmos.DrawCube(Selection.activeTransform.position, Vector3.one);
    }
}
