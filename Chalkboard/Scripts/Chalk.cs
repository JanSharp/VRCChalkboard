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
        [SerializeField] private ChalkboardManager chalkboardManager; // TODO: use OnBuild
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Transform debugIndicator;
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

        private Chalkboard lastSyncedChalkboard;
        private Chalkboard chalkboard;
        private Texture2D texture;

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
                if (chalkboard == null || hit.transform != chalkboard.transform)
                {
                    hasPrev = false;
                    chalkboard = hit.transform.GetComponent<Chalkboard>();
                    if (chalkboard == null)
                    {
                        debugIndicator.gameObject.SetActive(false);
                        return;
                    }
                    texture = chalkboard.texture;
                }
                debugIndicator.gameObject.SetActive(true);
                debugIndicator.position = hit.point;
                if (!holding)
                    return;
                var width = texture.width;
                var height = texture.height;
                var blPos = chalkboard.bottomLeft.position;
                var trPos = chalkboard.topRight.position;
                int x = (int)Mathf.Clamp(Mathf.Abs((hit.point.x - blPos.x) / (trPos.x - blPos.x)) * width, 2, width - 3);
                int y = (int)Mathf.Clamp(Mathf.Abs((hit.point.y - blPos.y) / (trPos.y - blPos.y)) * height, 2, height - 3);
                if (hasPrev && (Mathf.Abs(x - prevX) + Mathf.Abs(y - prevY)) <= 2) // didn't draw more than 2 pixels from prev point? => ignore
                    return;
                AddPointToSyncedPoints(x, y);
                DrawFromPrevTo(x, y);
                chalkboard.UpdateTextureFast();
            }
            else
            {
                debugIndicator.gameObject.SetActive(false);
            }
        }

        private void DrawFromPrevTo(int toX, int toY)
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
                            DrawPoint(x, Mathf.RoundToInt(y += stepY));
                    else
                        for (int x = prevX + stepX; x >= toX + 1; x += stepX)
                            DrawPoint(x, Mathf.RoundToInt(y += stepY));
                }
                else // vertical
                {
                    int stepY = System.Math.Sign(delta.y) * 2;
                    float stepX = (delta.x / Mathf.Abs(delta.y)) * 2;
                    float x = prevX;
                    if (prevY < toY)
                        for (int y = prevY + stepY; y <= toY - 1; y += stepY)
                            DrawPoint(Mathf.RoundToInt(x += stepX), y);
                    else
                        for (int y = prevY + stepY; y >= toY + 1; y += stepY)
                            DrawPoint(Mathf.RoundToInt(x += stepX), y);
                }
            }
            DrawPoint(toX, toY);
            hasPrev = true;
            prevX = toX;
            prevY = toY;
        }

        private void DrawPoint(int x, int y)
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
            bool changedBoard = chalkboard != lastSyncedChalkboard;
            if ((changedBoard ? pointsStageCount + 1 : pointsStageCount) >= pointsStage.Length)
            {
                var newPointsStage = new int[pointsStageCount * 2];
                pointsStage.CopyTo(newPointsStage, 0);
                pointsStage = newPointsStage;
            }
            if (changedBoard)
            {
                lastSyncedChalkboard = chalkboard;
                Debug.Log($"<dlt> adding switch to board id: {chalkboard.boardId}");
                // this works because it makes y == 0 which is an invalid for a point
                // so we can detect that x is actually a board id when y == 0
                pointsStage[pointsStageCount++] = chalkboard.boardId;
            }
            Debug.Log($"<dlt> adding point x: {x}, y: {y}, hasPrev: {hasPrev}");
            pointsStage[pointsStageCount++] = x | (y << AxisBitCount) | (hasPrev ? IntHasPrev : 0);
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            Debug.Log($"<dlt> sending {System.Math.Min(pointsStageCount, 3)} points (or switches)");
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
            bool doUpdateTexture = false;
            for (int i = 0; i < 3; i++)
            {
                int point = (int)((syncedData >> (i * PointBitCount)) & PointBits);
                if (point == IntUnusedPoint)
                    break;
                doUpdateTexture = true;
                int x = point & IntAxisBits;
                int y = (point >> AxisBitCount) & IntAxisBits;
                if (y == 0)
                {
                    Debug.Log($"<dlt> received switch to board id: {x}");
                    if (i != 0 && lastSyncedChalkboard != null) // update the previous texture before switching
                        lastSyncedChalkboard.UpdateTextureSlow();
                    lastSyncedChalkboard = chalkboardManager.chalkboards[x];
                    texture = lastSyncedChalkboard.texture;
                }
                else
                {
                    if ((point & IntHasPrev) == 0)
                        hasPrev = false;
                    Debug.Log($"<dlt> received point x: {x}, y: {y} hasPrev: {((point & IntHasPrev) != 0)}");
                    DrawFromPrevTo(x, y);
                }
            }
            if (doUpdateTexture && lastSyncedChalkboard != null) // null check just in case there is some edge case with wrong order of packets
                lastSyncedChalkboard.UpdateTextureSlow();
        }
    }
}
