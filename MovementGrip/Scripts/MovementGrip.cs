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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MovementGrip : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        , IOnBuildCallback
    #endif
    {
        public Transform toMove;
        public bool allowMovementOnX;
        public bool allowMovementOnY;
        public bool allowMovementOnZ;
        public float maxRightDeviation = float.PositiveInfinity;
        public float maxLeftDeviation = float.PositiveInfinity;
        public float maxUpDeviation = float.PositiveInfinity;
        public float maxDownDeviation = float.PositiveInfinity;
        public float maxForwardDeviation = float.PositiveInfinity;
        public float maxBackDeviation = float.PositiveInfinity;

        [SerializeField] [HideInInspector] private UpdateManager updateManager;
        [SerializeField] [HideInInspector] private VRC_Pickup pickup;
        [SerializeField] [HideInInspector] private Vector3 targetInitialLocalPosition;
        [SerializeField] [HideInInspector] private Vector3 thisInitialLocalPosition;
        [SerializeField] [HideInInspector] private Quaternion rotationOffsetFromTargetToThis;
        [SerializeField] [HideInInspector] private Vector3 positionOffsetFromTargetToThis;
        // for UpdateManager
        private int customUpdateInternalIndex;

        #if UNITY_EDITOR && !COMPILER_UDONSHARP
        [InitializeOnLoad]
        public static class OnBuildRegister
        {
            static OnBuildRegister() => JanSharp.OnBuildUtil.RegisterType<MovementGrip>();
        }
        bool IOnBuildCallback.OnBuild()
        {
            pickup = GetComponent<VRC_Pickup>();
            updateManager = GameObject.Find("/UpdateManager")?.GetUdonSharpComponent<UpdateManager>();
            if (updateManager == null)
            {
                Debug.LogError("MovementGrip requires a GameObject that must be at the root of the scene "
                        + "with the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour.",
                    UdonSharpEditorUtility.GetBackingUdonBehaviour(this));
                return false;
            }
            targetInitialLocalPosition = toMove.localPosition;
            thisInitialLocalPosition = this.transform.localPosition;
            positionOffsetFromTargetToThis = toMove.InverseTransformDirection(this.transform.position - toMove.position);
            rotationOffsetFromTargetToThis = Quaternion.Inverse(toMove.rotation) * this.transform.rotation;
            SnapBack();
            this.ApplyProxyModifications();
            return true;
        }
        #endif

        private void Snap(Vector3 localOffset, Quaternion rotationOffset)
        {
            this.transform.SetPositionAndRotation(
                toMove.position + toMove.TransformDirection(localOffset),
                toMove.rotation * rotationOffset
            );
        }

        private void SnapBack() => Snap(positionOffsetFromTargetToThis, rotationOffsetFromTargetToThis);

        public override void OnPickup()
        {
            // HoldingPlayer = Networking.LocalPlayer;
            // Networking.SetOwner(HoldingPlayer, this.gameObject);
            // isReceiving = false;
            // currentlyHeld = true;
            // currentHandBone = pickup.currentHand == VRC_Pickup.PickupHand.Right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            // RequestSerialization();
            updateManager.Register(this);
        }

        public override void OnDrop()
        {
            // currentlyHeld = false;
            // RequestSerialization();
            SnapBack();
            updateManager.Deregister(this);
        }

        public void CustomUpdate()
        {
            var direction = toMove.InverseTransformDirection(this.transform.localPosition - thisInitialLocalPosition);

            if (allowMovementOnX)
                direction.x = Mathf.Clamp(direction.x, -maxLeftDeviation, maxRightDeviation);
            else
                direction.x = 0;

            if (allowMovementOnY)
                direction.y = Mathf.Clamp(direction.y, -maxDownDeviation, maxUpDeviation);
            else
                direction.y = 0;

            if (allowMovementOnZ)
                direction.z = Mathf.Clamp(direction.z, -maxBackDeviation, maxForwardDeviation);
            else
                direction.z = 0;

            toMove.localPosition = targetInitialLocalPosition + direction;
        }
    }
}
