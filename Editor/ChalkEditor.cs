using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEditor;
using UdonSharpEditor;

namespace JanSharp
{
    [InitializeOnLoad]
    internal static class ChalkOnBuild
    {
        static ChalkOnBuild() => JanSharp.OnBuildUtil.RegisterType<Chalk>(OnBuild);

        private static bool OnBuild(Chalk chalk)
        {
            UpdateManager updateManager = GameObject.Find("/UpdateManager")?.GetComponent<UpdateManager>();
            ChalkboardManager chalkboardManager = GameObject.Find("/ChalkboardManager")?.GetComponent<ChalkboardManager>();

            if (updateManager == null)
                Debug.LogError("Chalk requires a GameObject that must be at the root of the scene with "
                    + "the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour.", chalk);

            if (chalkboardManager == null)
                Debug.LogError("Chalk requires a GameObject that must be at the root of the scene with the "
                    + "exact name 'ChalkboardManager' which has the 'ChalkboardManager' UdonBehaviour.", chalk);

            SerializedObject chalkProxy = new SerializedObject(chalk);
            chalkProxy.FindProperty(nameof(Chalk.updateManager)).objectReferenceValue = updateManager;
            chalkProxy.FindProperty(nameof(Chalk.chalkboardManager)).objectReferenceValue = chalkboardManager;
            chalkProxy.FindProperty(nameof(Chalk.chalkId)).intValue = chalkboardManager == null ? -1 : ChalkboardManagerOnBuild.GetChalkId(chalkboardManager, chalk);
            chalkProxy.ApplyModifiedProperties();

            return updateManager != null && chalkboardManager != null;
        }
    }
}
