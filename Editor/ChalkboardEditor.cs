using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using UnityEditor;
using UdonSharpEditor;

namespace JanSharp
{
    [InitializeOnLoad]
    internal static class ChalkboardOnBuildRegister
    {
        static ChalkboardOnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<Chalkboard>(OnBuild);

        private static bool OnBuild(Chalkboard chalkboard)
        {
            ChalkboardManager chalkboardManager = GameObject.Find("/ChalkboardManager")?.GetComponent<ChalkboardManager>();

            if (chalkboardManager == null)
                Debug.LogError("Chalkboard requires a GameObject that must be at the root of the scene with the "
                    + "exact name 'ChalkboardManager' which has the 'ChalkboardManager' UdonBehaviour.", chalkboard);

            SerializedObject chalkboardProxy = new SerializedObject(chalkboard);

            chalkboardProxy.FindProperty(nameof(Chalkboard.chalkboardManager)).objectReferenceValue = chalkboardManager;
            chalkboardProxy.FindProperty(nameof(Chalkboard.boardId)).intValue
                = chalkboardManager == null ? -1 : ChalkboardManagerOnBuild.GetBoardId(chalkboard.chalkboardManager, chalkboard);

            if (chalkboard.bottomLeft != null && chalkboard.topRight != null && chalkboard.material != null)
            {
                var blPos = chalkboard.bottomLeft.position;
                var trPos = chalkboard.topRight.position;

                var vertical = chalkboard.bottomLeft.up;
                var horizontal = chalkboard.bottomLeft.right;

                // blPos.x + X * horizontal.x + Y * vertical.x = trPos.x
                // blPos.y + X * horizontal.y + Y * vertical.y = trPos.y
                // blPos.z + X * horizontal.z + Y * vertical.z = trPos.z

                // blPos.x + X * horizontal.x + Y * vertical.x - trPos.x = 0
                // blPos.y + X * horizontal.y + Y * vertical.y - trPos.y = 0
                // blPos.z + X * horizontal.z + Y * vertical.z - trPos.z = 0

                Transform boardParent = chalkboard.bottomLeft.parent;
                chalkboardProxy.FindProperty(nameof(Chalkboard.boardParent)).objectReferenceValue = boardParent;
                if (chalkboard.topRight.parent != boardParent)
                    Debug.LogError($"{nameof(Chalkboard.bottomLeft)} and {nameof(Chalkboard.topRight)} must share the same parent", chalkboard);

                var texture = (Texture2D)chalkboard.material.mainTexture;
                var pixelsPerUnit = new Vector3(
                    ((chalkboard.topRight.localPosition.x - chalkboard.bottomLeft.localPosition.x) / texture.width),
                    ((chalkboard.topRight.localPosition.y - chalkboard.bottomLeft.localPosition.y) / texture.height)
                );

                var lossyScale = boardParent.lossyScale;

                Vector3 chalkScale = pixelsPerUnit * 5.75f;
                chalkboardProxy.FindProperty(nameof(Chalkboard.chalkScale)).vector3Value = new Vector3(lossyScale.x * chalkScale.x, lossyScale.y * chalkScale.y, 0.01f);
                Vector3 spongeScale = pixelsPerUnit * 41f;
                chalkboardProxy.FindProperty(nameof(Chalkboard.spongeScale)).vector3Value = new Vector3(lossyScale.x * spongeScale.x, lossyScale.y * spongeScale.y, 0.01f);
            }

            if (chalkboard.bottomLeft == null || chalkboard.topRight == null || chalkboard.material == null)
                Debug.LogError($"{nameof(Chalkboard.bottomLeft)}, {nameof(Chalkboard.topRight)} and {nameof(Chalkboard.material)} must all be set.", chalkboard);

            chalkboardProxy.ApplyModifiedProperties();

            return chalkboardManager != null;
        }
    }
}
