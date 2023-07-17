﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ChalkboardManager : UdonSharpBehaviour
    {
        [HideInInspector] public Chalkboard[] chalkboards;
        [HideInInspector] public Chalk[] chalks;
    }
}
