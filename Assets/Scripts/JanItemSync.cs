
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
    private Quaternion attachedRotationOffset;

    // state tracking to determine when the player and item held still for long enough to really determine the attached offset
    private const float ExpectedStillFrameCount = 2;
    private const float MagnitudeTolerance = 0.075f;
    private const float IntolerableMagnitudeDiff = 0.15f;
    private float stillFrameCount;
    private Vector3 prevBonePos;
    private Quaternion prevBoneRotation;
    private Vector3 prevItemPos;
    private Quaternion prevItemRotation;
    private bool heldStillLongEnough;

    // for the update manager
    private int customUpdateInternalIndex;

    public override void OnPickup()
    {
        Debug.Assert(pickup.IsHeld, "Picked up but not held?!");
        Debug.Assert(pickup.currentHand != VRC_Pickup.PickupHand.None, "Held but not in either hand?!");
        attachedBone = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        // boneTransform = pickup.currentPlayer.GetBoneTransform(attachedBone); // not exposed
        // Debug.Assert(boneTransform != null, "Didn't get the bone's transform.");
        updateManager.Register(this);

        prevBonePos = pickup.currentPlayer.GetBonePosition(attachedBone);
        prevBoneRotation = pickup.currentPlayer.GetBoneRotation(attachedBone);
        prevItemPos = this.transform.position;
        prevItemRotation = this.transform.rotation;

        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        visualTransform.SetPositionAndRotation(player.GetBonePosition(attachedBone), player.GetBoneRotation(attachedBone));
        attachedLocalOffset = visualTransform.InverseTransformDirection(prevItemPos - prevBonePos);
        Debug.Log($"Initial attached offset {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");

        attachedRotationOffset = Quaternion.Inverse(prevBoneRotation) * prevItemRotation;

        // this.transform.RotateAround(bonePosition, Quaternion.FromToRotation(boneRotation.axis))
    }

    public override void OnDrop()
    {
        updateManager.Deregister(this);
        stillFrameCount = 0;
        heldStillLongEnough = false;
    }

    private float lastTime;

    public void CustomUpdate()
    {
        // fetch values
        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        var bonePos = player.GetBonePosition(attachedBone);
        var boneRotation = player.GetBoneRotation(attachedBone);

        // move some transform to match the bone, because the TransformDirection methods require an instance of a Transform
        visualTransform.SetPositionAndRotation(bonePos, boneRotation);

        // determine item and player stillness and update the local offset accordingly
        if (heldStillLongEnough)
        {
            // I think this part still has to make sure the offset is about right, but we'll see

            // for debugging
            // var itemPos = this.transform.position;
            // var currentOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
            // var magnitudeDiff = (currentOffset - attachedLocalOffset).magnitude;
            // if (Time.time > lastTime + 1f)
            // {
            //     lastTime = Time.time;
            //     Debug.Log($"Current offset magnitude diff: {magnitudeDiff}.");
            // }
        }
        else
        {
            var itemPos = this.transform.position;
            var itemRotation = this.transform.rotation;
            if (PositionsAlmostEqual(prevItemPos, itemPos) && PositionsAlmostEqual(prevBonePos, bonePos))
            {
                stillFrameCount++;
                if (stillFrameCount == ExpectedStillFrameCount)
                {
                    heldStillLongEnough = true;
                    attachedLocalOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
                    attachedRotationOffset = Quaternion.Inverse(boneRotation) * itemRotation;
                    Debug.Log($"Held still long enough, setting local offset to {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");
                }
            }
            else
            {
                stillFrameCount = 0;
                // check the offsets anyway because the item could still be very far away, so we have to start moving it closer to the hand
                var currentOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
                var magnitudeDiff = (currentOffset - attachedLocalOffset).magnitude;
                if (magnitudeDiff >= IntolerableMagnitudeDiff)
                {
                    attachedLocalOffset = currentOffset;
                    Debug.Log($"Updating local pickup position, changed by {magnitudeDiff}, set to {attachedLocalOffset}, length {attachedLocalOffset.magnitude}");
                }
            }
            prevBonePos = bonePos;
            prevBoneRotation = boneRotation;
            prevItemPos = itemPos;
            prevItemRotation = itemRotation;
        }

        // move the item to the bone using the current local offset
        visualTransform.SetPositionAndRotation(
            bonePos + visualTransform.TransformDirection(attachedLocalOffset),
            boneRotation * attachedRotationOffset
        );
    }

    private bool PositionsAlmostEqual(Vector3 one, Vector3 two)
    {
        return (one - two).magnitude <= MagnitudeTolerance;
    }
}
