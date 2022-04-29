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

    public void OnEnable()
    {
        grid = (CustomGrid)target;
        grid.InitOrRestore();
        SceneView.duringSceneGui += GridUpdate;
    }

    void GridUpdate(SceneView sceneview)
    {
        Event e = Event.current;
        if (e.isKey && e.character == 'a')
        {
            if (Selection.activeGameObject)
            {
                Vector2Int gridPos = grid.GetMouseGridPosition(e.mousePosition);
                if (!grid.allObjects.TryGetValue(gridPos, out var existingObj) || existingObj == null)
                {
                    GameObject copy = Instantiate(Selection.activeGameObject);
                    copy.transform.position = grid.GridToWorld(gridPos);
                    grid.AddObject(gridPos, copy);
                }
            }
        }
    }

    // public override void OnInspectorGUI()
    // {
    // }
}
