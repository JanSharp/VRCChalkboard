using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGrid : MonoBehaviour
{
    public int drawnGridRadius = 100;
    public float gridSize = 1f;
    public Vector2 origin = new Vector2(0, 0); // i don't care enough to fix this right now

    void OnDrawGizmos()
    {
        Camera camera = Camera.current;
        Vector3 camPos = GridToWorld(GetMouseGridPosition(new Vector2(camera.pixelWidth / 2f, camera.pixelHeight / 2f)));
        float camX = camPos.x;
        float camY = camPos.y;
        float actualGridRadius = drawnGridRadius * gridSize;
        float startOffset = gridSize / 2f - actualGridRadius;
        float stopOffset = -startOffset + 0.000001f; // for floating point errors
        for (float x = origin.x + camX - startOffset; x <= origin.y + camX + stopOffset; x += gridSize)
            Gizmos.DrawLine(new Vector3(x, camY - startOffset), new Vector3(x, camY + stopOffset));
        for (float y = origin.y + camY - startOffset; y <= origin.y + camY + stopOffset; y += gridSize)
            Gizmos.DrawLine(new Vector3(camX - startOffset, y), new Vector3(camX + stopOffset, y));
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int( // the -1 in the negatives is needed because casting to int truncates
            (int)((worldPos.x - origin.x) / gridSize) - (worldPos.x < 0 ? 1 : 0),
            (int)((worldPos.y - origin.y) / gridSize) - (worldPos.y < 0 ? 1 : 0)
        );
    }

    public Vector3 GridToWorld(Vector2Int girdPos)
    {
        return new Vector3(
            origin.x + girdPos.x * gridSize + gridSize / 2f,
            origin.y + girdPos.y * gridSize + gridSize / 2f
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
