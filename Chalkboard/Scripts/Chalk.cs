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
        }

        public override void OnPickupUseDown()
        {
            holding = true;
        }

        public override void OnPickupUseUp()
        {
            holding = false;
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
                float x = Mathf.Abs((hit.point.x - board.bottomLeft.position.x) / (board.topRight.position.x - board.bottomLeft.position.x));
                float y = Mathf.Abs((hit.point.y - board.bottomLeft.position.y) / (board.topRight.position.y - board.bottomLeft.position.y));
                x = (int)(x * texture.width);
                y = (int)(y * texture.height);
                int xPixelBL = System.Math.Max(0, System.Math.Min(texture.width - 1, (int)(x - 2)));
                int yPixelBL = System.Math.Max(0, System.Math.Min(texture.height - 1, (int)(y - 2)));
                int xPixelTR = System.Math.Max(0, System.Math.Min(texture.width - 1, (int)(x + 2)));
                int yPixelTR = System.Math.Max(0, System.Math.Min(texture.height - 1, (int)(y + 2)));
                colors[0] = texture.GetPixel(xPixelBL, yPixelBL);
                colors[4] = texture.GetPixel(xPixelTR, yPixelBL);
                colors[24 - 4] = texture.GetPixel(xPixelBL, yPixelTR);
                colors[24 - 0] = texture.GetPixel(xPixelTR, yPixelTR);
                // FIXME: this can go out of bounds
                texture.SetPixels(xPixelBL, yPixelBL, 5, 5, colors);
                texture.Apply();
            }
            else
            {
                debugIndicator.gameObject.SetActive(false);
            }
        }
    }
}
