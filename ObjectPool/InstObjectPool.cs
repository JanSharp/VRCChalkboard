using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InstObjectPool : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        [SerializeField] [HideInInspector] private GameObject original;
        [SerializeField] [HideInInspector] private Vector3 originalLocalPosition;
        [SerializeField] [HideInInspector] private Quaternion originalLocalRotation;
        [SerializeField] [HideInInspector] private Transform activeParent;
        [SerializeField] [HideInInspector] private Transform inactiveParent;
        [SerializeField] [HideInInspector] private int activeCount;
        private int totalCount = 1;

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<InstObjectPool>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            if (this.transform.childCount == 2)
            {
                activeParent = this.transform.GetChild(0);
                inactiveParent = this.transform.GetChild(1);
                original = (activeParent.childCount == 0 ? inactiveParent.GetChild(0) : activeParent.GetChild(0)).gameObject;
            }
            else if (this.transform.childCount == 1)
            {
                original = this.transform.GetChild(0).gameObject;
                Transform MakeEmpty(string name)
                {
                    var result = Instantiate(new GameObject(), this.transform.position, this.transform.rotation, this.transform).transform;
                    result.name = name;
                    return result;
                }
                activeParent = MakeEmpty("ActiveParent");
                inactiveParent = MakeEmpty("InactiveParent");
            }
            else
            {
                Debug.LogError("Expected single child for Inst Object Pool which would be the item to be pooled.");
                return false;
            }
            original.transform.parent = original.activeSelf ? activeParent : inactiveParent;
            originalLocalPosition = original.transform.localPosition;
            originalLocalRotation = original.transform.localRotation;
            activeCount = original.activeSelf ? 1 : 0;
            this.ApplyProxyModifications();
            return original != null;
        }
        #endif

        public void Spawn()
        {
            Transform transform;
            if (activeCount == totalCount)
            {
                var obj = VRCInstantiate(original);
                obj.SetActive(true);
                obj.name = original.name + totalCount;
                transform = obj.transform;
                transform.parent = activeParent;
                totalCount++;
            }
            else
            {
                transform = inactiveParent.GetChild(0);
                transform.gameObject.SetActive(true);
                transform.parent = activeParent;
            }
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            activeCount++;
        }

        public void DeSpawn(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.parent = inactiveParent;
            activeCount--;

            // this is the problem
            // we can't implement this properly
            // we need a fast way to look up an object
            // where all we have is arrays and GameObjects
            // well we dod have Unity
            // we can technically turn this into an O(1) operation
            // we can change parents
            // yea, I think that's what I end up having to do
            // because I simply don't have a way to lookup an object in an array without looping through it
            // I just hope that unity implemented their stuff properly, which they probably haven't
            // but one can hope, right?
        }
    }
}
