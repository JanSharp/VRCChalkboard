
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class JanItemSync : UdonSharpBehaviour
{
    public UpdateManager updateManager;
    public VRC_Pickup pickup;

    public GameObject bonePositionVisualization;

    private HumanBodyBones attachedBone;

    // for the update manager
    private int customUpdateInternalIndex;

    public override void OnPickup()
    {
        Debug.Log("picked up");
        Debug.Assert(pickup.IsHeld, "Picked up but not held?!");
        Debug.Assert(pickup.currentHand != VRC_Pickup.PickupHand.None, "Held but not in either hand?!");
        attachedBone = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        // boneTransform = pickup.currentPlayer.GetBoneTransform(attachedBone);
        // Debug.Assert(boneTransform != null, "Didn't get the bone's transform.");
        updateManager.Register(this);
    }

    public override void OnDrop()
    {
        Debug.Log("dropped");
        updateManager.Deregister(this);
    }

    public void CustomUpdate()
    {
        Debug.Log("updating");
        bonePositionVisualization.transform.position = pickup.currentPlayer.GetBonePosition(attachedBone);
    }
}
