using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Chalkboard : UdonSharpBehaviour
    {
        public Transform bottomLeft;
        public Transform topRight;
        public Renderer boardRenderer;
        public float chalkPixelRadius;
    }
}
