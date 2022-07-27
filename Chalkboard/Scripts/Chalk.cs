using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
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

        [SerializeField] private Transform debugIndicator;

        public override void OnPickup()
        {
            updateManager.Register(this);
            colors = new Color[5 * 5];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = color;
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
    }
}
