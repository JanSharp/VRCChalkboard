﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Spin : UdonSharpBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * 50f);
    }
}