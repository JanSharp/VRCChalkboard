
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
    private Vector3 attachedLocalOffset;

    // for the update manager
    private int customUpdateInternalIndex;

    public override void OnPickup()
    {
        Debug.Assert(pickup.IsHeld, "Picked up but not held?!");
        Debug.Assert(pickup.currentHand != VRC_Pickup.PickupHand.None, "Held but not in either hand?!");
        attachedBone = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        // boneTransform = pickup.currentPlayer.GetBoneTransform(attachedBone);
        // Debug.Assert(boneTransform != null, "Didn't get the bone's transform.");
        updateManager.Register(this);

        Vector3 bonePosition = pickup.currentPlayer.GetBonePosition(attachedBone);
        Quaternion boneRotation = pickup.currentPlayer.GetBoneRotation(attachedBone);
        Vector3 thisPosition = transform.position;
        Quaternion thisRotation = transform.rotation;

        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        visualTransform.SetPositionAndRotation(player.GetBonePosition(attachedBone), player.GetBoneRotation(attachedBone));
        attachedLocalOffset = visualTransform.InverseTransformDirection(thisPosition - bonePosition);

        // figure out local offset
        // unity can already do this, but I don't think I have a way to make the held item a child of the bone of the vrc model
        // this.transform.RotateAround(bonePosition, Quaternion.FromToRotation(boneRotation.axis))
        // my brain is melting from this, I have not learned any of this before
        // rotations are... ridiculously difficult. Like I think I need matrix math for this, but I got no clue
        // nor have I ever used matrix math either
    }

    private float lastUpdate;
    public void Update()
    {
        if (Time.time > lastUpdate + 2f)
        {
            lastUpdate = Time.time;
            Debug.Log($"Parent object name: {this.transform.parent.name}");
        }
    }

    public override void OnDrop()
    {
        updateManager.Deregister(this);
    }

    public void CustomUpdate()
    {
        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        var rotation = player.GetBoneRotation(attachedBone);
        visualTransform.SetPositionAndRotation(player.GetBonePosition(attachedBone), rotation);
        visualTransform.SetPositionAndRotation(visualTransform.TransformDirection(attachedLocalOffset), rotation);
        // bonePositionVisualization.transform.position = pickup.currentPlayer.GetBonePosition(attachedBone);
        // bonePositionVisualization.transform.rotation = pickup.currentPlayer.GetBoneRotation(attachedBone);
    }
}
