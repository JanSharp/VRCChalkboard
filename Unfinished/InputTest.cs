using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("Input.GetKeyDown(KeyCode.LeftShift)");
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("Input.GetKey(KeyCode.LeftShift)");
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("Input.GetKeyDown(KeyCode.Tab)");
        }
        if (Input.GetKey(KeyCode.Tab))
        {
            Debug.Log("Input.GetKey(KeyCode.Tab)");
        }
    }
}
