/*
MIT License

Copyright (c) 2022 Jan Mischak

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
                Undo.RegisterCreatedObjectUndo(obj, "Create grid aligned object");
                Undo.IncrementCurrentGroup();
                e.Use(); // maybe don't? eh we'll see
            }
        }
    }

    // public override void OnInspectorGUI()
    // {
    // }
}
