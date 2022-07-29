using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Chalkboard : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        public Transform bottomLeft;
        public Transform topRight;
        public Material material;
        public Color boardColor;

        [HideInInspector] public int boardId;
        [HideInInspector] [System.NonSerialized] public Texture2D texture;
        private Color[] initialPixels;
        private bool fastUpdating;
        private bool slowUpdating;
        private ulong[] allActions;
        private int allActionsCount;
        private ulong currentActions;
        private int currentActionsIndex;

        private void Start()
        {
            texture = (Texture2D)material.mainTexture;
            // ok so this seems to be using about 9MB for a 1024x512 texture
            // if there was no overhead that should be 2MB
            // note that this is also based on one single test in the editor with 3 boards in the scene
            // however considering this saves us having to loop through all pixels to reset the color
            // well I really can't say it is a good option, but it is an option that allows us to
            // reset the board to the initial state in 2ms instead of 1.5s which it is when looping
            // through all pixels to reset them takes (first a GetPixels call, then the loop, then SetPixels)
            initialPixels = texture.GetPixels();
        }

        // fast is kept completely separate from slow because when multiple people are drawing
        // we wouldn't want to inconsistently switch between slow and fast updates
        // instead it'll just update a bit quicker 4 times per frame while updating 15 times
        // per frame in general, so a total of ~19 updates. Still irregular but at least
        // it's updating quickly and the logic is simple

        public void UpdateTextureFast()
        {
            if (fastUpdating)
                return;
            fastUpdating = true;
            SendCustomEventDelayedSeconds(nameof(UpdateTextureFastDelayed), 1f / 15f);
        }

        public void UpdateTextureFastDelayed()
        {
            texture.Apply();
            fastUpdating = false;
        }

        public void UpdateTextureSlow()
        {
            if (slowUpdating)
                return;
            slowUpdating = true;
            SendCustomEventDelayedSeconds(nameof(UpdateTextureSlowDelayed), 1f / 4f);
        }

        public void UpdateTextureSlowDelayed()
        {
            texture.Apply();
            slowUpdating = false;
        }

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<Chalkboard>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            var chalkboardManager = GameObject.Find("/ChalkboardManager")?.GetUdonSharpComponent<ChalkboardManager>();
            boardId = chalkboardManager?.GetBoardId(this) ?? -1;
            if (chalkboardManager == null)
                Debug.LogError("Chalkboard requires a GameObject that must be at the root of the scene"
                        + " with the exact name 'ChalkboardManager' which has the 'ChalkboardManager' UdonBehaviour.",
                    UdonSharpEditorUtility.GetBackingUdonBehaviour(this));

            this.ApplyProxyModifications();
            // EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
            return chalkboardManager != null;
        }
        #endif

        public void Clear()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ClearInternal));
        }

        public void ClearInternal()
        {
            texture.SetPixels(initialPixels);
            allActionsCount = 0;
            currentActionsIndex = 0;
            UpdateTextureSlow();
        }



        public void DrawPoint(Chalk chalk, int x, int y)
        {
            DrawPointInternal(chalk, x, y);
        }

        public void DrawLine(Chalk chalk, int fromX, int fromY, int toX, int toY)
        {
            DrawLineInternal(chalk, fromX, fromY, toX, toY);
        }

        private void DrawPointInternal(Chalk chalk, int x, int y)
        {
            if (chalk.isSponge)
            {
                texture.SetPixels(x - chalk.halfSize, y - chalk.halfSize, chalk.size, chalk.size, chalk.colors);
            }
            else
            {
                int blX = x - chalk.halfSize;
                int blY = y - chalk.halfSize;
                int trX = x + chalk.halfSize;
                int trY = y + chalk.halfSize;
                chalk.colors[0] = texture.GetPixel(blX, blY);
                chalk.colors[4] = texture.GetPixel(trX, blY);
                chalk.colors[20] = texture.GetPixel(blX, trY);
                chalk.colors[24] = texture.GetPixel(trX, trY);
                texture.SetPixels(blX, blY, chalk.size, chalk.size, chalk.colors);
            }
        }

        private void DrawLineInternal(Chalk chalk, int fromX, int fromY, int toX, int toY)
        {
            Vector2 delta = new Vector2(toX - fromX, toY - fromY);
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) // horizontal
            {
                int stepX = System.Math.Sign(delta.x) * chalk.lineDrawingFrequency;
                float stepY = (delta.y / Mathf.Abs(delta.x)) * chalk.lineDrawingFrequency;
                float y = fromY;
                if (fromX < toX)
                    for (int x = fromX + stepX; x <= toX - 1; x += stepX)
                        DrawPoint(chalk, x, Mathf.RoundToInt(y += stepY));
                else
                    for (int x = fromX + stepX; x >= toX + 1; x += stepX)
                        DrawPoint(chalk, x, Mathf.RoundToInt(y += stepY));
            }
            else // vertical
            {
                int stepY = System.Math.Sign(delta.y) * chalk.lineDrawingFrequency;
                float stepX = (delta.x / Mathf.Abs(delta.y)) * chalk.lineDrawingFrequency;
                float x = fromX;
                if (fromY < toY)
                    for (int y = fromY + stepY; y <= toY - 1; y += stepY)
                        DrawPoint(chalk, Mathf.RoundToInt(x += stepX), y);
                else
                    for (int y = fromY + stepY; y >= toY + 1; y += stepY)
                        DrawPoint(chalk, Mathf.RoundToInt(x += stepX), y);
            }
            DrawPointInternal(chalk, toX, toY);
        }
    }
}
