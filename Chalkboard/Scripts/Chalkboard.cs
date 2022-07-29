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
            UpdateTextureSlow();
        }
    }
}
