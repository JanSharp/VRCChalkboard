using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomGrid))]
public class GridEditor : Editor
{
    // public override void OnInspectorGUI()
    // {
    // }

    private CustomGrid grid;

    public void OnEnable()
    {
        grid = (CustomGrid)target;
        SceneView.duringSceneGui += GridUpdate;
    }

    void GridUpdate(SceneView sceneview)
    {
        Event e = Event.current;
        if (e.isKey && e.character == 'a')
        {
            if (Selection.activeGameObject)
            {
                Vector3 mousePos = grid.GetMousePosition(e.mousePosition);
                Vector2Int gridPos = grid.WorldToGrid(mousePos);
                Vector3 pos = grid.GridToWorld(gridPos);
                // Debug.Log($"mouse position: {mousePos}, grid position: {gridPos}, pos: {pos}");
                GameObject copy = Instantiate(Selection.activeGameObject);
                copy.transform.position = pos;
            }
        }
    }
}
