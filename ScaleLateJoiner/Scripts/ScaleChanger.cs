using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ScaleChanger : UdonSharpBehaviour
    {
        public ItemScaleManager scaleManager;
        private Transform Target;
        [UdonSynced]
        private int TargetId;
        public string Name;
        private Vector3 startScale;
        [UdonSynced]
        private Vector3 syncScale;
        public float Percentage = .1f;
        private bool isAdd;
        private bool isSubtract;
        public void OnTriggerEnter(Collider other)
        {
            if (other.name.Contains("Item") || other.name.Contains("Alchemy") || other.name.Contains("Ingredient") || other.name.Contains("Coin"))
            {
                Target = other.transform;
                Name = other.name;
                startScale = new Vector3(Target.localScale.x, Target.localScale.y, Target.localScale.z);
                TargetId = scaleManager.GetIdForItem(Target.gameObject);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.name.Contains(Name))
            {
                Name = "";
                Target = null;
            }
        }

        public void onAdd()
        {
            if (Target != null)
            {
                isAdd = true;
                syncScale = Target.localScale * (1f + Percentage);
                Target.localScale = syncScale;
                Sync();
            }
        }

        public void onSubtract()
        {
            if (Target != null)
            {
                if (Target.localScale.x > 0)
                {
                    isSubtract = true;
                    syncScale = Target.localScale * (1f - Percentage);
                    Target.localScale = syncScale;
                    Sync();
                }
            }
        }

        //public void Update()
        //{
        //    if (Input.GetButton("Oculus_CrossPlatform_SecondaryIndexTrigger") && (isAdd == true || isSubtract == true))
        //    {
        //        if (isAdd == true)
        //        {
        //            Sync();
        //            syncScale = new Vector3(Target.localScale.x + (startScale.x * Percentage), Target.localScale.y + (startScale.y * Percentage), Target.localScale.z + (startScale.z * Percentage));
        //            Target.localScale = syncScale;
        //        }
        //        if (isSubtract == true)
        //        {
        //            Sync();
        //            syncScale = new Vector3(Target.localScale.x - (startScale.x * Percentage), Target.localScale.y - (startScale.y * Percentage), Target.localScale.z - (startScale.z * Percentage));
        //            Target.localScale = syncScale;
        //        }
        //    }
        //    if (Input.GetButtonUp("Oculus_CrossPlatform_SecondaryIndexTrigger") && (isAdd == true || isSubtract == true))
        //    {
        //        isAdd = false;
        //        isSubtract = false;
        //    }
        //}
        public void Sync()
        {
            scaleManager.SetScale(TargetId, syncScale);
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            if (Target != null)
            {
                Target.localScale = syncScale;
                scaleManager.SetScale(TargetId, syncScale);
            }
        }
    }
}
