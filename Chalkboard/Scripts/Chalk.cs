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
        // for UpdateManager
        private int customUpdateInternalIndex;
        private bool holding;

        [SerializeField] private Transform debugIndicator;

        public override void OnPickup()
        {
            updateManager.Register(this);
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
                // NOTE: this algorithm is not very performant
                for (float xCheck = Mathf.Floor(-board.chalkPixelRadius); xCheck <= Mathf.Ceil(board.chalkPixelRadius); xCheck++)
                    for (float yCheck = Mathf.Floor(-board.chalkPixelRadius); yCheck <= Mathf.Ceil(board.chalkPixelRadius); yCheck++)
                        if (Mathf.Sqrt(xCheck * xCheck + yCheck * yCheck) <= board.chalkPixelRadius)
                        {
                            int xPixel = System.Math.Max(0, System.Math.Min(texture.width - 1, (int)(xCheck + x * texture.width)));
                            int yPixel = System.Math.Max(0, System.Math.Min(texture.height - 1, (int)(yCheck + y * texture.height)));
                            texture.SetPixel(xPixel, yPixel, Color.white);
                        }
                texture.Apply();
            }
            else
            {
                debugIndicator.gameObject.SetActive(false);
            }
        }
    }
}
