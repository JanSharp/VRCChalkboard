
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

// TODO: fixed positions
// TODO: disallowed item theft support

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class JanItemSync : UdonSharpBehaviour
{
    public UpdateManager updateManager;
    public VRC_Pickup pickup;
    public Transform dummyTransform;

    private const byte IdleState = 0; // the only state with CustomUpdate deregistered
    private const byte FadeInState = 1;
    private const byte WaitingForStillnessState = 2;
    private const byte WaitingForLongerStillnessState = 3;
    private const byte SendingState = 4;
    private const byte ReceivingState = 5;
    private byte state = IdleState;

    private VRCPlayerApi attachedPlayer;
    private VRCPlayerApi AttachedPlayer
    {
        get => attachedPlayer;
        set
        {
            attachedPlayer = value;
            attachedPlayerIsInVR = value.IsUserInVR();
        }
    }
    private bool attachedPlayerIsInVR;
    private HumanBodyBones attachedBone;
    private Vector3 attachedLocalOffset;
    private Quaternion attachedRotationOffset;

    // first bit being 1 indicates the item is attached
    // second bit is used when attached, 0 means attached to right hand, 1 means left hand
    [UdonSynced] private byte syncedFlags;
    [UdonSynced] private Vector3 syncedPosition;
    [UdonSynced] private Quaternion syncedRotation;
    // 29 bytes (1 + 12 + 16) worth of data, and we get 84 bytes as the byte count in OnPostSerialization. I'll leave it at that

    // state tracking to determine when the player and item held still for long enough to really determine the attached offset
    private const float ExpectedStillFrameCount = 5;
    private const float ExpectedLongerStillFrameCount = 20;
    private const float MagnitudeTolerance = 0.075f;
    private const float IntolerableMagnitudeDiff = 0.15f;
    private const float IntolerableAngleDiff = 30f;
    private const float FadeInTime = 2f; // seconds of manual position syncing before attaching
    private float stillFrameCount;
    private Vector3 prevBonePos;
    private Quaternion prevBoneRotation;
    private Vector3 prevItemPos;
    private Quaternion prevItemRotation;
    private float pickupTime;

    // for the update manager
    private int customUpdateInternalIndex;

    public override void OnPickup()
    {
        // this method can actually handle any previous state simply because it resets all state required for the states after FadeInState
        // and IdleState and ReceivingState don't need any cleanup, they just get overwritten

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
        AttachedPlayer = Networking.LocalPlayer;

        var visualTransform = dummyTransform.transform;
        visualTransform.SetPositionAndRotation(prevBonePos, prevBoneRotation);
        attachedLocalOffset = visualTransform.InverseTransformDirection(prevItemPos - prevBonePos);
        Debug.Log($"Initial attached offset {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");

        attachedRotationOffset = Quaternion.Inverse(prevBoneRotation) * prevItemRotation;

        RegisterCustomUpdate();
        // technically redundant because the VRCPickup script already does this, but I do not trust it nor do I trust order of operation
        Networking.SetOwner(AttachedPlayer, this.gameObject);
        SendChanges();

        stillFrameCount = 0;
        state = FadeInState;
        pickupTime = Time.time;
    }

    public override void OnDrop()
    {
        // if we already switched to receiving state before this player dropped this item don't do anything
        if (state == ReceivingState)
            return;
        DeregisterCustomUpdate();
        SendChanges();
        state = IdleState;
    }

    public void CustomUpdate()
    {
        if (state == IdleState)
        {
            Debug.LogError($"It should truly be impossible for CustomUpdate to run when an item is in IdleState. Item name: ${this.name}.");
            return;
        }
        if (state == ReceivingState)
            UpdateReceiver();
        else
            UpdateSender();
    }

    ///cSpell:ignore jank
    // alright, so I've decided. For now I'm going to ignore theft and simply declare it undefined
    // and even if/once I handle item theft I'm not going to use VRCPickup for it, I'm going to check if it's allowed
    // and the prevent theft myself, because I have no interest in quite literally telling every client that that player picked up an item
    // because I can already see just how jank and hard it would be to synchronize local position and rotation. It would be pure pain
    // that means, however, we need to know if it is possible to disable a pickup script temporarily
    // I'll have to figure that out once this script has been fully refactored and tested

    private void UpdateSender()
    {
        // states to handle:
        // FadeInState
        // WaitingForStillnessState
        // WaitingForLongerStillnessState
        // SendingState

        if (state == SendingState)
        {
            // I think this part still has to make sure the offset is about right, but we'll see
            // it'll definitely have to sync rotation in desktop mode, not sure if that's possible in VR
            return;
        }
        
        if (state == FadeInState)
        {
            if (Time.time > pickupTime + FadeInTime)
                state = WaitingForStillnessState;
            SendChanges(); // simply sending changes is going to send the current item position and rotation
        }
        else if (state == WaitingForStillnessState)
            SendChanges(); // regardless of what happens this update, it will send changes

        // fetch values
        var player = pickup.currentPlayer;
        var bonePos = player.GetBonePosition(attachedBone);
        var boneRotation = player.GetBoneRotation(attachedBone);

        // move some transform to match the bone, because the TransformDirection methods require an instance of a Transform
        dummyTransform.SetPositionAndRotation(bonePos, boneRotation);

        // determine item and player stillness and update the local offset accordingly
        var itemPos = this.transform.position;
        var itemRotation = this.transform.rotation;
        if (PositionsAlmostEqual(prevItemPos, itemPos) && PositionsAlmostEqual(prevBonePos, bonePos))
        {
            stillFrameCount++;
            if ((state == WaitingForLongerStillnessState || state == WaitingForStillnessState) && stillFrameCount >= ExpectedLongerStillFrameCount)
            {
                attachedLocalOffset = dummyTransform.InverseTransformDirection(itemPos - bonePos);
                attachedRotationOffset = Quaternion.Inverse(boneRotation) * itemRotation;
                Debug.Log($"Held still even longer, setting local offset to {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");
                state = SendingState;
                SendChanges();
            }
            else if (state == WaitingForStillnessState && stillFrameCount >= ExpectedStillFrameCount)
            {
                attachedLocalOffset = dummyTransform.InverseTransformDirection(itemPos - bonePos);
                attachedRotationOffset = Quaternion.Inverse(boneRotation) * itemRotation;
                Debug.Log($"Held still long enough, setting local offset to {attachedLocalOffset} with length {attachedLocalOffset.magnitude}.");
                state = WaitingForLongerStillnessState;
                SendChanges();
            }
        }
        else
        {
            stillFrameCount = 0;

            // this logic is now redundant due to the fade in logic
            // if (state == FadeInState || state == WaitingForStillnessState) // no need to adjust positions yet since it's not even attached yet
            //     return;
            // // check the offsets anyway because the item could still be very far away, so we have to start moving it closer to the hand
            // var currentOffset = dummyTransform.InverseTransformDirection(itemPos - bonePos);
            // var magnitudeDiff = (currentOffset - attachedLocalOffset).magnitude;

            // var currentRotationOffset = Quaternion.Inverse(boneRotation) * itemRotation;
            // var rotationDiff = Quaternion.Inverse(currentRotationOffset) * attachedRotationOffset;
            // float angle;
            // Vector3 axis;
            // rotationDiff.ToAngleAxis(out angle, out axis);

            // if (magnitudeDiff >= IntolerableMagnitudeDiff || angle >= IntolerableAngleDiff)
            // {
            //     attachedLocalOffset = currentOffset;
            //     attachedRotationOffset = currentRotationOffset;
            //     Debug.Log($"Updating local pickup position, changed by {magnitudeDiff}, set to {attachedLocalOffset}, length {attachedLocalOffset.magnitude}.");
            //     Debug.Log($"Updating local rotation, changed by {angle} degrees around the axis {axis}.");
            //     SendChanges();
            // }
        }
        prevBonePos = bonePos;
        prevBoneRotation = boneRotation;
        prevItemPos = itemPos;
        prevItemRotation = itemRotation;

        // debugging: move the dummy object to the bone using the current local offset
        // dummyTransform.SetPositionAndRotation(
        //     bonePos + dummyTransform.TransformDirection(attachedLocalOffset),
        //     boneRotation * attachedRotationOffset
        // );
    }

    private bool PositionsAlmostEqual(Vector3 one, Vector3 two)
    {
        return (one - two).magnitude <= MagnitudeTolerance;
    }

    private void UpdateReceiver()
    {
        // prevent this object from being moved by this logic if the local player is holding it
        // we might not have gotten the OnPickup event before the (this) Update event yet
        if (pickup.IsHeld)
            return;

        // fetch values
        var bonePos = AttachedPlayer.GetBonePosition(attachedBone);
        var boneRotation = AttachedPlayer.GetBoneRotation(attachedBone);

        // move some transform to match the bone, because the TransformDirection methods
        // require an instance of a Transform and we can't get the bone's Transform directly
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
        if (state == ReceivingState)
        {
            // TODO: uh idk what to do, shouldn't this be impossible?
        }
        syncedFlags = 0;
        if (state == WaitingForLongerStillnessState || state == SendingState)
        {
            syncedFlags += 1; // set attached flag
            syncedPosition = attachedLocalOffset;
            syncedRotation = attachedRotationOffset;
            if (attachedBone == HumanBodyBones.LeftHand)
                syncedFlags += 2; // set left hand flag, otherwise it's right hand
        }
        else //if (state == IdleState || state == FadeInState || state == WaitingForStillnessState)
        {
            syncedPosition = this.transform.position;
            syncedRotation = this.transform.rotation;
        }
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        if (!result.success)
        {
            Debug.LogWarning($"Syncing request was dropped for {this.name}, trying again.");
            SendChanges(); // TODO: somehow test if this kind of retry even works or if the serialization request got reset right afterwards
        }
        // else
        //     Debug.Log($"Sending {result.byteCount} bytes");
    }

    public override void OnDeserialization()
    {
        if ((syncedFlags & 1) != 0) // is attached?
        {
            attachedBone = (syncedFlags & 2) != 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            attachedLocalOffset = syncedPosition;
            attachedRotationOffset = syncedRotation;
            AttachedPlayer = Networking.GetOwner(this.gameObject);
            RegisterCustomUpdate();
            state = ReceivingState;
        }
        else // not attached
        {
            this.transform.SetPositionAndRotation(syncedPosition, syncedRotation);
            DeregisterCustomUpdate();
            state = IdleState;
        }
    }

    /// <summary>
    /// Call this before updating the state
    /// </summary>
    private void RegisterCustomUpdate()
    {
        if (state == IdleState)
            updateManager.Register(this);
    }

    /// <summary>
    /// Call this before updating the state
    /// </summary>
    private void DeregisterCustomUpdate()
    {
        if (state != IdleState)
            updateManager.Deregister(this);
    }
}
