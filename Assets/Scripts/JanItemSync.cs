
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

    // state tracking to determine when the player and item held still for long enough to really determine the attached offset
    private const float ExpectedStillFrameCount = 5;
    private const float MagnitudeTolerance = 0.075f;
    private const float IntolerableMagnitudeDiff = 0.15f;
    private float stillFrameCount;
    private Vector3 prevBonePos;
    private Vector3 prevItemPos;
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
        Quaternion boneRotation = pickup.currentPlayer.GetBoneRotation(attachedBone);
        prevItemPos = this.transform.position;
        Quaternion itemRotation = this.transform.rotation;

        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        visualTransform.SetPositionAndRotation(player.GetBonePosition(attachedBone), player.GetBoneRotation(attachedBone));
        attachedLocalOffset = visualTransform.InverseTransformDirection(prevItemPos - prevBonePos);
        Debug.Log($"Initial attached offset {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");

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
            var itemPos = this.transform.position;
            var currentOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
            var magnitudeDiff = (currentOffset - attachedLocalOffset).magnitude;
            if (Time.time > lastTime + 1f)
            {
                lastTime = Time.time;
                Debug.Log($"Current offset magnitude diff: {magnitudeDiff}.");
            }
        }
        else
        {
            var itemPos = this.transform.position;
            if (PositionsAlmostEqual(prevItemPos, itemPos) && PositionsAlmostEqual(prevBonePos, bonePos))
            {
                stillFrameCount++;
                if (stillFrameCount == ExpectedStillFrameCount)
                {
                    heldStillLongEnough = true;
                    attachedLocalOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
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
        }

        // move the item to the bone using the current local offset
        visualTransform.position = bonePos + visualTransform.TransformDirection(attachedLocalOffset);
        // TODO: handle relative rotation
    }

    private bool PositionsAlmostEqual(Vector3 one, Vector3 two)
    {
        return (one - two).magnitude <= MagnitudeTolerance;
    }
}
