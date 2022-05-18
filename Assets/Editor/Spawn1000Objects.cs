using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn1000Objects : MonoBehaviour
{
    [UnityEditor.MenuItem("Custom/Spawn1000Objects")]
    static void Spawn()
    {
        GameObject original = GameObject.Find("ObjectToSpawn");
        for (int i = 0; i < 1000; i++)
        {
            Instantiate(original, new Vector3(5f, 0.5f, i - 500f), original.transform.rotation);
        }
    }
}
