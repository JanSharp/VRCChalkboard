
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PointToTransform : UdonSharpBehaviour
{
    public Transform target;
    public void Update()
    {
        var dir = target.transform.position - this.transform.position;
        dir.y = 0;
        this.transform.rotation = Quaternion.LookRotation(dir);
    }
}
