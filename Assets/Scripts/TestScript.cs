using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdonSharp;
using TMPro;
using VRC.Udon;

///cSpell:ignore UGUI

public class TestScript : UdonSharpBehaviour
{
    // // private string GetString(bool foo)
    // // {
    // //     return foo ? "yes" : "no";
    // // }

    // void Update()
    // {
    //     // // Debug.Log(GetString(true));
    //     // // Debug.Log(GetString(false));

    //     // string[] createdArray = new string[4];
    //     // Debug.Log(createdArray[1]);
    // }



    void Start()
    {
        OnChangeTest();
        TextMeshProTest();
    }



    public TextMeshPro textObj;
    public TextMeshProUGUI textUGUIObj;

    void TextMeshProTest()
    {
        // alright, so instead of using the cursed function/method SetText
        // it's a simple assignment to the text field (property, yea ignore those)
        // that's it. it's this simple
        textObj.text = "one";
        textUGUIObj.text = "two";
        textObj.text = (1).ToString();
    }



    [FieldChangeCallback(nameof(OnChangePointTotal))] // mark it for on change
    public int pointTotal;

    // the on change node
    private int OnChangePointTotal
    {
        set
        {
            // the old value is still in `bar`
            // the new value is in the special keyword `value`
            // according to the description on FieldChangeCallback this method
            // is responsible for setting bar = value
            Debug.Log("old:" + pointTotal);
            Debug.Log("new:" + value);
            pointTotal = value; // yep, tested it, this is necessary,
            // otherwise the pointTotal value will not be changed
        }
    }

    // wizardry
    private int pointTotalProxy {
        get => pointTotal;
        set => SetProgramVariable(nameof(pointTotal), value);
    }

    void OnChangeTest()
    {
        pointTotal = 100; // doesn't run OnChangePointTotal
        SetProgramVariable(nameof(pointTotal), 200); // does run OnChangePointTotal
        Debug.Log("after setting:" + pointTotal);
        pointTotalProxy = 400;
        Debug.Log("proxy: " + pointTotalProxy);
    }

    // explain "custom events" with parameters and return values
    // explain local variables
    // optional:
    // if statements without {} - single statement bodies
    // same for else












































    public GameObject target;
    void Event1()
    {
        // order of operation:
        // 4            1      3            2                       6               5
        ((UdonBehaviour)target.GetComponent(typeof(UdonBehaviour))).SendCustomEvent("hello");
    }

    private UdonBehaviour example;
    void Event2()
    {
        example = (UdonBehaviour)target.GetComponent(typeof(UdonBehaviour));
        example.SendCustomEvent("hello");
    }

    void Event3()
    {
        // local variables in order to reuse values without bloating fields in the entire class
        UdonBehaviour behaviour = (UdonBehaviour)target.GetComponent(typeof(UdonBehaviour));
        behaviour.SetProgramVariable("something", "some value");
        behaviour.SendCustomEvent("event");
    }

    public GameObject[] targetArray;
    // the array is basically an association of indexes to values, so
    // 0 => GameObject
    // 1 => GameObject
    // 2 => GameObject
    // 3 => GameObject
    // 4 => GameObject
    // 5 => GameObject
    // 6 => GameObject
    // 7 => GameObject
    void Event4()
    {
        GameObject obj0 = (GameObject)targetArray.GetValue(0);
        obj0.SetActive(false);
    }

    void Event5()
    {
        GameObject obj0 = targetArray[0];
        obj0.SetActive(false);
    }

    void Event6()
    {
        targetArray.SetValue(target, 1);
    }

    void Event7()
    {
        targetArray[1] = target;
    }

    void Event8()
    {
        //   make var ; loop while this is true; increment
        for (int i = 0; i < targetArray.Length; i++)
        {
            ((GameObject)targetArray.GetValue(i)).SetActive(false);
        }
    }

    void Event9()
    {
        //   make var ; loop while this is true; increment
        for (int i = 0; i < targetArray.Length; i++)
        {
            // for reference:
            // the hard way:
            //   GameObject obj0 = (GameObject)targetArray.GetValue(0);
            // the easy way:
            //   GameObject obj0 = targetArray[0];
            targetArray[i].SetActive(false);
        }
    }

    void Event10()
    {
        int theSizeWeWant = 100; // evaluated somehow
        GameObject[] myArray = new GameObject[theSizeWeWant];
        myArray[0] = target;

        string[] strings = new string[]
        {
            "one",
            "two",
            "three",
        };
    }

    public bool condition;
    void Event11()
    {
        if (condition)
        {
            Debug.Log("if");
            Debug.Log("and another statement inside the block");
        }
        else
        {
            Debug.Log("else");
            Debug.Log("and again some more stuff");
        }

        if (condition)
            Debug.Log("if");
            // putting another statement here wouldn't be apart of the if block anymore
        else
            Debug.Log("else");
            // same here

        Debug.Log("and again some more stuff"); // this is outside of the else block
    }

    // stuff you might want to learn today:
    // - arrays :check:
    // - for loops :check:
    // - function calls with parameters and return values (new!!)
    // - while loops
}
