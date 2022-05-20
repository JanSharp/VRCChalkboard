
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class JanItemSync : UdonSharpBehaviour
{
    public UpdateManager updateManager;
    public VRC_Pickup pickup;

    // debugging
    public GameObject bonePositionVisualization;
    // private float lastTime;

    private HumanBodyBones attachedBone;
    private Vector3 attachedLocalOffset;
    private Quaternion attachedRotationOffset;

    [UdonSynced]
    private byte syncedSmallData;
    [UdonSynced]
    private Vector3 syncedPosition;
    [UdonSynced]
    private Quaternion syncedRotation;

    // state tracking to determine when the player and item held still for long enough to really determine the attached offset
    private const float ExpectedStillFrameCount = 4;
    private const float MagnitudeTolerance = 0.075f;
    private const float IntolerableMagnitudeDiff = 0.15f;
    private const float IntolerableAngleDiff = 30f;
    private const float FadeInTime = 1f; // seconds of manual position syncing before attaching
    private float stillFrameCount;
    private Vector3 prevBonePos;
    private Quaternion prevBoneRotation;
    private Vector3 prevItemPos;
    private Quaternion prevItemRotation;
    private bool heldStillLongEnough;
    private float pickupTime;

    // for the update manager
    private int customUpdateInternalIndex;

    // update manager related
    private bool customUpdateIsRegistered;

    public override void OnPickup()
    {
        Debug.Assert(pickup.IsHeld, "Picked up but not held?!");
        Debug.Assert(pickup.currentHand != VRC_Pickup.PickupHand.None, "Held but not in either hand?!");
        attachedBone = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        // boneTransform = pickup.currentPlayer.GetBoneTransform(attachedBone); // not exposed
        // Debug.Assert(boneTransform != null, "Didn't get the bone's transform.");

        var player = pickup.currentPlayer;
        prevBonePos = player.GetBonePosition(attachedBone);
        prevBoneRotation = player.GetBoneRotation(attachedBone);
        prevItemPos = this.transform.position;
        prevItemRotation = this.transform.rotation;

        var visualTransform = bonePositionVisualization.transform;
        visualTransform.SetPositionAndRotation(prevBonePos, prevBoneRotation);
        attachedLocalOffset = visualTransform.InverseTransformDirection(prevItemPos - prevBonePos);
        Debug.Log($"Initial attached offset {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");

        attachedRotationOffset = Quaternion.Inverse(prevBoneRotation) * prevItemRotation;

        RegisterCustomUpdate();
        SendChanges();

        pickupTime = Time.time;
    }

    public override void OnDrop()
    {
        stillFrameCount = 0;
        heldStillLongEnough = false;
        bonePositionVisualization.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
        DeregisterCustomUpdate();
        SendChanges();
    }

    public void CustomUpdate()
    {
        if (Time.time < pickupTime + FadeInTime && pickup.IsHeld)
        {
            // TODO: manually sync position
        }
        else
        {
            if (Networking.IsOwner(this.gameObject))
                UpdateSender();
            else
                UpdateReceiver();
        }
    }

    private void UpdateSender()
    {
        // fetch values
        var player = pickup.currentPlayer;
        var visualTransform = bonePositionVisualization.transform;
        var bonePos = player.GetBonePosition(attachedBone);
        var boneRotation = player.GetBoneRotation(attachedBone);

        // move some transform to match the bone, because the TransformDirection methods require an instance of a Transform
        visualTransform.SetPositionAndRotation(bonePos, boneRotation);

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
            // determine item and player stillness and update the local offset accordingly
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
                    SendChanges();
                }
            }
            else
            {
                stillFrameCount = 0;
                // check the offsets anyway because the item could still be very far away, so we have to start moving it closer to the hand
                var currentOffset = visualTransform.InverseTransformDirection(itemPos - bonePos);
                var magnitudeDiff = (currentOffset - attachedLocalOffset).magnitude;

                var currentRotationOffset = Quaternion.Inverse(boneRotation) * itemRotation;
                var rotationDiff = Quaternion.Inverse(currentRotationOffset) * attachedRotationOffset;
                float angle;
                Vector3 axis;
                rotationDiff.ToAngleAxis(out angle, out axis);

                if (magnitudeDiff >= IntolerableMagnitudeDiff || angle >= IntolerableAngleDiff)
                {
                    attachedLocalOffset = currentOffset;
                    attachedRotationOffset = currentRotationOffset;
                    Debug.Log($"Updating local pickup position, changed by {magnitudeDiff}, set to {attachedLocalOffset}, length {attachedLocalOffset.magnitude}.");
                    Debug.Log($"Updating local rotation, changed by {angle} degrees around the axis {axis}.");
                    SendChanges();
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

    private void UpdateReceiver()
    {
        // prevent this object from being moved by this logic if the local player is holding it
        // the local player should now be the owner, but it might take a few frames for ownership to transfer
        if (pickup.IsHeld)
            return;

        // fetch values
        var player = Networking.GetOwner(this.gameObject);
        var bonePos = player.GetBonePosition(attachedBone);
        var boneRotation = player.GetBoneRotation(attachedBone);

        // move some transform to match the bone, because the TransformDirection methods require an instance of a Transform
        this.transform.SetPositionAndRotation(bonePos, boneRotation);
        this.transform.SetPositionAndRotation(
            bonePos + this.transform.TransformDirection(attachedLocalOffset),
            boneRotation * attachedRotationOffset
        );
    }

    private void SendChanges()
    {
        RequestSerialization();
    }

    public override void OnPreSerialization()
    {
        syncedSmallData = 0;
        if (pickup.IsHeld)
        {
            syncedSmallData += 1;
            syncedPosition = attachedLocalOffset;
            syncedRotation = attachedRotationOffset;
        }
        else
        {
            syncedPosition = this.transform.position;
            syncedRotation = this.transform.rotation;
        }
        if (attachedBone == HumanBodyBones.LeftHand)
            syncedSmallData += 2;
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (!result.success)
        {
            Debug.LogWarning($"Syncing request was dropped for {this.name}, trying again.");
            SendChanges(); // TODO: somehow test if this kind of retry even works or if the serialization request got reset right afterwards
        }
        else
            Debug.Log($"Sending {result.byteCount} bytes");
    }

    public override void OnDeserialization()
    {
        if ((syncedSmallData & 1) != 0) // is attached?
        {
            attachedBone = (syncedSmallData & 2) != 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            attachedLocalOffset = syncedPosition;
            attachedRotationOffset = syncedRotation;
            RegisterCustomUpdate();
        }
        else // not attached
        {
            this.transform.SetPositionAndRotation(syncedPosition, syncedRotation);
            DeregisterCustomUpdate();
        }
    }

    private void RegisterCustomUpdate()
    {
        if (!customUpdateIsRegistered)
        {
            updateManager.Register(this);
            customUpdateIsRegistered = true;
        }
    }

    private void DeregisterCustomUpdate()
    {
        if (customUpdateIsRegistered)
        {
            updateManager.Deregister(this);
            customUpdateIsRegistered = false;
        }
    }
}
