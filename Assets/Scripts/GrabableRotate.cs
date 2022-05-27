
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GrabableRotate : UdonSharpBehaviour
{
    [SerializeField]
    private Transform toRotate;

    private UpdateManager updateManager;
    private VRC_Pickup pickup;
    private float initialDistance;
    private float nextSyncTime;
    private const float SyncInterval = 0.2f;

    [UdonSynced] private bool currentlyHeld;
    [UdonSynced] private Quaternion syncedRotation;

    // for UpdateManager
    private int customUpdateInternalIndex;

    public void Start()
    {
        pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        var updateManagerObj = GameObject.Find("/UpdateManager");
        updateManager = updateManagerObj == null ? null : (UpdateManager)updateManagerObj.GetComponent(typeof(UdonBehaviour));
        if (updateManager == null)
            Debug.LogError("GrabableRotate requires a GameObject that must be at the root of the scene with the exact name 'UpdateManager' which has the 'UpdateManager' UdonBehaviour.");
        // initialOffset = this.transform.position - toRotate.position;
        // initialRotation = toRotate.rotation;
        initialDistance = (this.transform.position - toRotate.position).magnitude;
        SnapBack();
    }

    private void SnapBack()
    {
        this.transform.position = toRotate.position + (toRotate.rotation * (Vector3.back * initialDistance));
    }

    public override void OnPickup()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        currentlyHeld = true;
        RequestSerialization();
        updateManager.Register(this);
        nextSyncTime = Time.time + SyncInterval;
    }

    public override void OnDrop()
    {
        currentlyHeld = false;
        RequestSerialization();
        SnapBack();
        updateManager.Deregister(this);
    }

    public void CustomUpdate()
    {
        toRotate.LookAt(toRotate.position - (this.transform.position - toRotate.position));
        if (Time.time >= nextSyncTime)
        {
            RequestSerialization();
            nextSyncTime = Time.time + SyncInterval;
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        // just for safety because I do not trust VRC
        // would be hilarious if this ended up running after OnDeserialization
        // causing the entire logic to be pointless/broken. Funny indeed
        pickup.pickupable = true;
    }

    public override void OnPreSerialization()
    {
        syncedRotation = toRotate.rotation;
    }

    public override void OnDeserialization()
    {
        toRotate.rotation = syncedRotation;
        pickup.pickupable = !currentlyHeld;
    }
}
