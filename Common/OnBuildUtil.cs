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
using System.Diagnostics;

namespace JanSharp
{
    [InitializeOnLoad]
    [DefaultExecutionOrder(-1000)]
    public static class OnBuildUtil
    {
        private static Dictionary<Type, OnBuildCallbackData> typesToLookFor;
        private static List<OnBuildCallbackData> typesToLookForList;

        static OnBuildUtil()
        {
            typesToLookFor = new Dictionary<Type, OnBuildCallbackData>();
            typesToLookForList = new List<OnBuildCallbackData>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange data)
        {
            if (data == PlayModeStateChange.ExitingEditMode)
                RunOnBuild();
        }

        public static void RegisterType<T>(int order = 0) where T : IOnBuildCallback
        {
            if (!typesToLookFor.ContainsKey(typeof(T)))
            {
                OnBuildCallbackData data = new OnBuildCallbackData(typeof(T), order);
                typesToLookFor.Add(typeof(T), data);
                typesToLookForList.Add(data);
            }
        }

        public static bool RunOnBuild()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (OnBuildCallbackData data in typesToLookForList)
                data.behaviours.Clear();

            foreach (GameObject obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (UdonBehaviour udonBehaviour in obj.GetComponentsInChildren<UdonBehaviour>())
                    if (UdonSharpEditorUtility.IsUdonSharpBehaviour(udonBehaviour))
                    {
                        UdonSharpBehaviour behaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);
                        if (typesToLookFor.TryGetValue(behaviour.GetType(), out OnBuildCallbackData data))
                            data.behaviours.Add(behaviour);
                    }

            foreach (OnBuildCallbackData data in typesToLookForList.OrderBy(d => d.order))
                foreach (UdonSharpBehaviour behaviour in data.behaviours)
                    if (!((IOnBuildCallback)behaviour).OnBuild())
                        return false;

            sw.Stop();
            UnityEngine.Debug.Log($"OnBuild handlers: {sw.Elapsed}.");
            return true;
        }

        private class OnBuildCallbackData
        {
            public Type type;
            public List<UdonSharpBehaviour> behaviours;
            public int order;

            public OnBuildCallbackData(Type type, int order)
            {
                this.type = type;
                this.behaviours = new List<UdonSharpBehaviour>();
                this.order = order;
            }
        }
    }

    public interface IOnBuildCallback
    {
        bool OnBuild();
    }

    ///cSpell:ignore IVRCSDK, VRCSDK

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
