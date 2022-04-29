using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGrid : MonoBehaviour
{
    public bool showGrid = true;
    public int drawnGridRadius = 100;
    public float gridSize = 1f;
    public GameObject prefab;
    public GameObject outputParent;
    public Vector2 gridOrigin = new Vector2(0, 0);
    public char keybind = 'a';

    [HideInInspector]
    [SerializeField]
    private List<Vector2Int> allObjectKeys;
    [HideInInspector]
    [SerializeField]
    private List<GameObject> allObjectValues;
    // unity can't serialize dictionaries, so we have to use lists for the keys and values along side the dictionary
    public Dictionary<Vector2Int, GameObject> allObjects;

    void Awake()
    {
        InitOrRestore();
    }

    public void InitOrRestore()
    {
        allObjects = new Dictionary<Vector2Int, GameObject>();
        if (allObjectKeys != null && allObjectValues != null)
        {
            for (int i = Math.Min(allObjectKeys.Count, allObjectValues.Count) - 1; i >= 0; i--)
            {
                if (allObjectValues[i] == null)
                {
                    allObjectKeys.RemoveAt(i);
                    allObjectValues.RemoveAt(i);
                }
                else
                {
                    allObjects[allObjectKeys[i]] = allObjectValues[i];
                }
            }
        }
        else
        {
            allObjectKeys = new List<Vector2Int>();
            allObjectValues = new List<GameObject>();
        }
        UpdateAllObjectPositions();
    }

    void OnValidate()
    {
        UpdateAllObjectPositions();
    }

    public void UpdateAllObjectPositions()
    {
        if (allObjects != null)
        {
            foreach (var kvp in allObjects)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.transform.position = GridToWorld(kvp.Key);
                }
            }
        }
    }

    public void AddObject(Vector2Int gridPos, GameObject obj)
    {
        allObjectKeys.Add(gridPos);
        allObjectValues.Add(obj);
        allObjects[gridPos] = obj;
    }

    void OnDrawGizmos()
    {
        if (!showGrid || gridSize < 0.0001f)
            return;
        Camera camera = Camera.current;
        Vector3 camPos = GridToWorld(GetMouseGridPosition(new Vector2(camera.pixelWidth / 2f, camera.pixelHeight / 2f)));
        float camX = camPos.x;
        float camY = camPos.y;
        float actualGridRadius = drawnGridRadius * gridSize;
        float startOffset = -(gridSize / 2f + actualGridRadius);
        float stopOffset = -startOffset + 0.001f; // for floating point errors
        for (float x = camX + startOffset; x <= camX + stopOffset; x += gridSize)
            Gizmos.DrawLine(
                new Vector3(x, camY + startOffset),
                new Vector3(x, camY + stopOffset)
            );
        for (float y = camY + startOffset; y <= camY + stopOffset; y += gridSize)
            Gizmos.DrawLine(
                new Vector3(camX + startOffset, y),
                new Vector3(camX + stopOffset, y)
            );
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int( // the -1 in the negatives is needed because casting to int truncates
            (int)((worldPos.x - gridOrigin.x) / gridSize) - (worldPos.x < 0 ? 1 : 0),
            (int)((worldPos.y - gridOrigin.y) / gridSize) - (worldPos.y < 0 ? 1 : 0)
        );
    }

    public Vector3 GridToWorld(Vector2Int girdPos)
    {
        return new Vector3(
            gridOrigin.x + girdPos.x * gridSize + gridSize / 2f,
            gridOrigin.y + girdPos.y * gridSize + gridSize / 2f
        );
    }

    public Vector3 GetMousePosition(Vector2 mousePosition)
    {
        Camera camera = Camera.current;
        Vector3 mouseScreenPos = new Vector3(mousePosition.x, camera.pixelHeight - mousePosition.y);
        mouseScreenPos.z = 1;
        Vector3 first = camera.ScreenToWorldPoint(mouseScreenPos);
        mouseScreenPos.z = 2;
        Vector3 second = camera.ScreenToWorldPoint(mouseScreenPos);
        Vector3 diff = second - first;
        float multiplier = Mathf.Abs(first.z / diff.z);
        Vector3 result = first + diff * multiplier;
        result.z = 0; // remove floating point errors
        return result;
    }

    public Vector2Int GetMouseGridPosition(Vector2 mousePosition) => WorldToGrid(GetMousePosition(mousePosition));
}
