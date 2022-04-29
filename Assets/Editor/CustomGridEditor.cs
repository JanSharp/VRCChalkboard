using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomGrid))]
public class GridEditor : Editor
{
    private CustomGrid grid;

    void OnEnable()
    {
        grid = (CustomGrid)target;
        grid.InitOrRestore();
        SceneView.duringSceneGui += GridUpdate;
    }

    void GridUpdate(SceneView sceneview)
    {
        Event e = Event.current;
        if (e.isKey && e.character == grid.keybind)
        {
            if (grid.prefab == null || grid.outputParent == null)
                return;

            Vector2Int gridPos = grid.GetMouseGridPosition(e.mousePosition);
            if (!grid.allObjects.TryGetValue(gridPos, out var existingObj) || existingObj == null)
            {
                GameObject obj = Instantiate(grid.prefab, grid.GridToWorld(gridPos), Quaternion.identity, grid.outputParent.transform);
                obj.name = grid.prefab.name;
                grid.AddObject(gridPos, obj);
                e.Use(); // maybe don't? eh we'll see
            }
        }
    }

    // public override void OnInspectorGUI()
    // {
    // }
}
