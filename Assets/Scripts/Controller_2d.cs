
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

///cSpell:ignore chipz

public class Controller_2d : UdonSharpBehaviour
{
    public Rigidbody2D characterRB;
    public Animator characterAnim;
    public UdonBehaviour chipz2DHP;
    public float speed;
    public float jumpImpulse;
    private bool isScarChan;
    [FieldChangeCallback(nameof(onHorizontalValueChange))]
    private float horizontalValue;

    private float onHorizontalValueChange
    {
        set
        {
            characterAnim.SetFloat("Speed", Mathf.Abs(value));
            horizontalValue = value;
        }
    }

    public void Update()
    {
        HandleDirection(KeyCode.RightArrow, 1f);
        HandleDirection(KeyCode.LeftArrow, -1f);
    }

    public void HandleDirection(KeyCode keycode, float direction)
    {
        if (Input.GetKey(keycode))
        {
            characterRB.transform.rotation = new Quaternion(0, 0, 0, 0);
            characterRB.velocity = new Vector2(speed * direction, characterRB.velocity.y);
            SetProgramVariable(nameof(horizontalValue), characterRB.velocity.x);
        }
        if (Input.GetKeyUp(keycode))
        {
            characterRB.velocity = new Vector2(0, characterRB.velocity.y);
            SetProgramVariable(nameof(horizontalValue), 0f);
        }
    }
}
