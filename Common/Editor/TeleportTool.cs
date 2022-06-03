using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

[EditorTool("Teleport Tool")]
public class TeleportTool : EditorTool
{
    GUIContent iconContent;
    public override GUIContent toolbarIcon => iconContent;
    private int controlID;
    private int layerMask = -1;
    private float distance = 0.5f;
    private string[] layerMaskNames;

    void OnEnable()
    {
        iconContent = new GUIContent()
        {
            image = null,
            text = "Teleport Tool",
            tooltip = "Teleport selected objects to where you click, facing away from the collider the ray hit.",
        };
        controlID = GUIUtility.GetControlID(FocusType.Passive);
        layerMaskNames = new string[32];
        for (int i = 0; i < 32; i++)
            layerMaskNames[i] = LayerMask.LayerToName(i);
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
            return;
        List<GameObject> objs = targets.Where(t => t is GameObject obj && !PrefabUtility.IsPartOfPrefabAsset(obj)).Cast<GameObject>().ToList();

        Handles.BeginGUI();
        using (new GUILayout.HorizontalScope())
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                distance = EditorGUILayout.FloatField(
                    new GUIContent("Distance", "The distance the teleported objects should have away from the clicked object."),
                    distance
                );
                layerMask = EditorGUILayout.MaskField(
                    new GUIContent("LayerMask", "Layers the ray used to figure out where to place the object should be placed should hit."),
                    layerMask,
                    layerMaskNames
                );
            }
            GUILayout.FlexibleSpace();
        }
        Handles.EndGUI();

        var e = Event.current;
        switch (e.type)
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(controlID);
                break;
            case EventType.MouseUp:
                Vector2 mousePos = e.mousePosition;
                mousePos.y = sceneView.camera.pixelHeight - mousePos.y;
                if (objs.Any() && Physics.Raycast(sceneView.camera.ScreenPointToRay(mousePos), out var hit, 1000f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    foreach (var obj in objs)
                    {
                        Undo.RecordObject(obj.transform, "Teleport Objects");
                        obj.transform.SetPositionAndRotation(hit.point + hit.normal * distance, Quaternion.LookRotation(hit.normal, Vector3.up));
                    }
                    Undo.IncrementCurrentGroup();
                }
                e.Use();
                break;
            case EventType.Repaint:
                foreach (var obj in objs)
                    Handles.DoPositionHandle(obj.transform.position, obj.transform.rotation);
                break;
        }
    }
}
