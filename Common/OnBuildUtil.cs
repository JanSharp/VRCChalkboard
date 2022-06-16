#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor.Build;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class OnBuildUtil
    {
        private static HashSet<Type> typesToLookFor;

        static OnBuildUtil()
        {
            typesToLookFor = new HashSet<Type>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange data)
        {
            if (data == PlayModeStateChange.ExitingEditMode)
                RunOnBuild();
        }

        public static void RegisterType<T>() where T : IOnBuildCallback
        {
            if (!typesToLookFor.Contains(typeof(T)))
                typesToLookFor.Add(typeof(T));
        }

        public static bool RunOnBuild()
        {
            foreach (GameObject obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (UdonBehaviour udonBehaviour in obj.GetComponentsInChildren<UdonBehaviour>())
                {
                    if (UdonSharpEditorUtility.IsUdonSharpBehaviour(udonBehaviour))
                    {
                        UdonSharpBehaviour behaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);
                        if (typesToLookFor.Contains(behaviour.GetType()) && !((IOnBuildCallback)behaviour).OnBuild())
                            return false;
                    }
                }
            return true;
        }
    }

    public interface IOnBuildCallback
    {
        bool OnBuild();
    }

    public class VRCOnBuild : IVRCSDKBuildRequestedCallback
    {
        int IOrderedCallback.callbackOrder => 0;

        bool IVRCSDKBuildRequestedCallback.OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
                return true;
            return OnBuildUtil.RunOnBuild();
        }
    }
}
#endif
