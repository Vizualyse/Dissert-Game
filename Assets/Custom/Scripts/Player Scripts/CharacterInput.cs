using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInput : MonoBehaviour
{
    public Vector2 input
    {
        get
        {
            Vector2 i = Vector2.zero;
            i.x = Input.GetAxis("Horizontal");
            i.y = Input.GetAxis("Vertical");
            i *= (i.x != 0.0f && i.y != 0.0f) ? .71f : 1.0f;
            return i;
        }
    }
    public bool run
    {
        get
        {
            return Input.GetButton("Run");
        }
    }

    public bool crouch
    {
        get
        {
            return Input.GetButtonDown("Crouch");     //make get ax
        }
    }

    private int jumpTimer;
    private bool jump;

    void Start()
    {
        jumpTimer = -1;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //smooths the input, jump triggers multiple times without this
        if (!Input.GetButton("Jump"))
        {
            jump = false;
            jumpTimer++;
        }
        else if (jumpTimer > 0)
            jump = true;
    }

    public bool Jump()
    {
        return jump;
    }

    public void ResetJump()
    {
        jumpTimer = -1;
    }
}
