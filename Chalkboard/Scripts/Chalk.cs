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

        private const int PointBitCount = 21;
        private const int AxisBitCount = 10;
        private const ulong PointHasPrev = 0x100000UL;
        private const ulong PointBits = 0x1fffffUL;
        private const ulong UnusedPoint = PointBits;
        [UdonSynced]
        private ulong syncedData;
        private int[] pointsStage = new int[4];
        private int pointsStageCount;
        private const int IntHasPrev = 0x100000;
        private const int IntPointBits = 0x1fffff;
        private const int IntUnusedPoint = IntPointBits;
        private const int IntAxisBits = 0x3ff;

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
                AddPointToSyncedPoints(x, y);
                DrawFromPrevTo(texture, x, y);
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
                var newPointsStage = new int[pointsStageCount * 2];
                pointsStage.CopyTo(newPointsStage, 0);
                pointsStage = newPointsStage;
            }
            Debug.Log($"<dlt> adding point x: {x}, y: {y}, hasPrev: {hasPrev}");
            pointsStage[pointsStageCount++] = x | (y << AxisBitCount) | (hasPrev ? IntHasPrev : 0);
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            Debug.Log($"<dlt> sending {System.Math.Min(pointsStageCount, 3)} points");
            if (pointsStageCount == 0)
            {
                syncedData = 0xffffffffffffffffUL;
                return;
            }
            syncedData = 0UL;
            for (int i = 0; i < 3; i++)
            {
                if (i < pointsStageCount)
                    syncedData |= ((ulong)(pointsStage[i] & 0x1fffffU)) << (i * PointBitCount);
                else
                    syncedData |= UnusedPoint << (i * PointBitCount);
            }

            if (pointsStageCount <= 3)
                pointsStageCount = 0;
            else
            {
                // TODO: improve the implementation of the que to not require any shifting
                for (int i = 3; i < pointsStageCount; i++)
                    pointsStage[i - 3] = pointsStage[i];
                pointsStageCount -= 3;
                SendCustomEventDelayedFrames(nameof(RequestSerializationDelayed), 1);
            }
        }

        public void RequestSerializationDelayed() => RequestSerialization();

        public override void OnPostSerialization(SerializationResult result)
        {
            Debug.Log($"<dlt> on post: success: {result.success}, byteCount: {result.byteCount}");
        }

        public override void OnDeserialization()
        {
            Texture2D texture = (Texture2D)chalkboard.boardRenderer.material.mainTexture;
            for (int i = 0; i < 3; i++)
            {
                int point = (int)((syncedData >> (i * PointBitCount)) & PointBits);
                if (point == IntUnusedPoint)
                    break;
                if ((point & IntHasPrev) == 0)
                    hasPrev = false;
                Debug.Log($"<dlt> received point x: {point & IntAxisBits}, y: {(point >> AxisBitCount) & IntAxisBits} hasPrev: {((point & IntHasPrev) != 0)}");
                DrawFromPrevTo(texture, point & IntAxisBits, (point >> AxisBitCount) & IntAxisBits);
            }
            texture.Apply();
        }
    }
}
