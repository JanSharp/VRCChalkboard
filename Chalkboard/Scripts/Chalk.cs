using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Chalk : UdonSharpBehaviour
    {
        [SerializeField] private Transform aimPoint;
        // 0: Default, 4: Water, 8: Interactive, 11: Environment
        [SerializeField] private LayerMask layerMask = (1 << 0) | (1 << 4) | (1 << 8) | (1 << 11);
        [SerializeField] private UpdateManager updateManager;
        [SerializeField] private Color color = Color.white;
        // for UpdateManager
        private int customUpdateInternalIndex;
        private bool holding;
        private bool hasPrev;
        private int prevX;
        private int prevY;

        private Color[] colors;

        [UdonSynced]
        private uint[] syncedPoints;
        private uint[] pointsStage = new uint[4];
        private int pointsStageCount;

        // TODO: sync which chalkboard is being drawn on
        [SerializeField] private Chalkboard chalkboard;
        [SerializeField] private Transform debugIndicator;

        private void Start()
        {
            colors = new Color[5 * 5];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = color;
        }

        public override void OnPickup()
        {
            updateManager.Register(this);
        }

        public override void OnDrop()
        {
            debugIndicator.gameObject.SetActive(false);
            updateManager.Deregister(this);
            // because I don't trust VRC
            holding = false;
            hasPrev = false;
        }

        public override void OnPickupUseDown()
        {
            holding = true;
        }

        public override void OnPickupUseUp()
        {
            holding = false;
            hasPrev = false;
        }

        public void CustomUpdate()
        {
            RaycastHit hit;
            if (Physics.Raycast(aimPoint.position, aimPoint.forward, out hit, 15f, layerMask.value) && hit.transform != null)
            {
                Chalkboard board = hit.transform.GetComponent<Chalkboard>();
                if (board == null)
                {
                    debugIndicator.gameObject.SetActive(false);
                    return;
                }
                debugIndicator.gameObject.SetActive(true);
                debugIndicator.position = hit.point;
                if (!holding)
                    return;
                Texture2D texture = (Texture2D)board.boardRenderer.material.mainTexture;
                var width = texture.width;
                var height = texture.height;
                var blPos = board.bottomLeft.position;
                var trPos = board.topRight.position;
                int x = (int)Mathf.Clamp(Mathf.Abs((hit.point.x - blPos.x) / (trPos.x - blPos.x)) * width, 2, width - 3);
                int y = (int)Mathf.Clamp(Mathf.Abs((hit.point.y - blPos.y) / (trPos.y - blPos.y)) * height, 2, height - 3);
                if (hasPrev && (Mathf.Abs(x - prevX) + Mathf.Abs(y - prevY)) <= 2) // didn't draw more than 2 pixels from prev point? => ignore
                    return;
                DrawFromPrevTo(texture, x, y);
                AddPointToSyncedPoints(x, y);
                texture.Apply();
            }
            else
            {
                debugIndicator.gameObject.SetActive(false);
            }
        }

        private void DrawFromPrevTo(Texture2D texture, int toX, int toY)
        {
            if (hasPrev)
            {
                Vector2 delta = new Vector2(toX - prevX, toY - prevY);
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) // horizontal
                {
                    int stepX = System.Math.Sign(delta.x) * 2;
                    float stepY = (delta.y / Mathf.Abs(delta.x)) * 2;
                    float y = prevY;
                    if (prevX < toX)
                        for (int x = prevX + stepX; x <= toX - 1; x += stepX)
                            DrawPoint(texture, x, Mathf.RoundToInt(y += stepY));
                    else
                        for (int x = prevX + stepX; x >= toX + 1; x += stepX)
                            DrawPoint(texture, x, Mathf.RoundToInt(y += stepY));
                }
                else // vertical
                {
                    int stepY = System.Math.Sign(delta.y) * 2;
                    float stepX = (delta.x / Mathf.Abs(delta.y)) * 2;
                    float x = prevX;
                    if (prevY < toY)
                        for (int y = prevY + stepY; y <= toY - 1; y += stepY)
                            DrawPoint(texture, Mathf.RoundToInt(x += stepX), y);
                    else
                        for (int y = prevY + stepY; y >= toY + 1; y += stepY)
                            DrawPoint(texture, Mathf.RoundToInt(x += stepX), y);
                }
            }
            DrawPoint(texture, toX, toY);
            hasPrev = true;
            prevX = toX;
            prevY = toY;
        }

        private void DrawPoint(Texture2D texture, int x, int y)
        {
            int blX = x - 2;
            int blY = y - 2;
            int trX = x + 2;
            int trY = y + 2;
            colors[0] = texture.GetPixel(blX, blY);
            colors[4] = texture.GetPixel(trX, blY);
            colors[20] = texture.GetPixel(blX, trY);
            colors[24] = texture.GetPixel(trX, trY);
            texture.SetPixels(blX, blY, 5, 5, colors);
        }

        private void AddPointToSyncedPoints(int x, int y)
        {
            if (pointsStageCount == pointsStage.Length)
            {
                var newPointsStage = new uint[pointsStageCount * 2];
                pointsStage.CopyTo(newPointsStage, 0);
                pointsStage = newPointsStage;
            }
            Debug.Log($"<dlt> adding point x: {x}, y: {y}");
            pointsStage[pointsStageCount++] = (((uint)x) << 16) + ((uint)y);
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            if (pointsStageCount == 0)
            {
                syncedPoints = null;
                return;
            }
            if (syncedPoints == null || syncedPoints.Length != pointsStageCount)
                syncedPoints = new uint[pointsStageCount];
            for (int i = 0; i < pointsStageCount; i++)
                syncedPoints[i] = pointsStage[i];
            Debug.Log($"<dlt> sending {pointsStageCount} points");
            pointsStageCount = 0;
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            Debug.Log($"<dlt> on post: success: {result.success}, byteCount: {result.byteCount}");
        }

        public override void OnDeserialization()
        {
            Debug.Log($"<dlt> received {(syncedPoints == null ? "null" : "not null")} points array");
            if (syncedPoints == null) // Just to make sure
                return;
            Texture2D texture = (Texture2D)chalkboard.boardRenderer.material.mainTexture;
            foreach (uint point in syncedPoints)
            {
                Debug.Log($"<dlt> received point x: {(int)((point >> 16) & 0xffff)}, y: {(int)(point & 0xffff)}");
                DrawFromPrevTo(texture, (int)((point >> 16) & 0xffff), (int)(point & 0xffff));
            }
            // TODO: better handling for "synced" `hasPrev`
            hasPrev = false;
            texture.Apply();
        }
    }
}
