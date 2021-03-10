using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideMove : MovementInterface
{
    public FloatRange slideSpeed = new FloatRange(7.0f, 12.0f);

    [SerializeField]
    float slideTime;
    float slideBlendTime = 0.222f;
    float slideDownward = 0f;

    [SerializeField]
    float minSlideSpeed = 5.0f;
    [SerializeField]
    float slideBoostAmount = 3f;

    [SerializeField]
    Vector3 slideDir;

    bool canSlide()
    {
        if (slideTime > 0 || playerState == changeTo) return false;
        if (movement.GetCurrentMovementSpeed() < minSlideSpeed) return false;
        return true;
    }

    public override void SetPlayerComponents(CharacterMovement move, CharacterInput input)
    {
        base.SetPlayerComponents(move, input);
    }
    public override void Movement()
    {
        if (movement.grounded && playerInput.Jump())
        {
            slideDir = transform.forward;
            if (player.Uncrouch()) { 
                movement.Jump(slideDir + Vector3.up, 1f);
                playerInput.ResetJump();
                slideTime = 0;
                player.ChangeState(State.RUNNING);
            }
        }


        movement.Move(slideDir, 1f, slideDir.y);
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract) return;

        //Unslide
        if (playerInput.crouch && player.state == State.SLIDING)   
        {
            if (player.Uncrouch())
            {
                slideTime = 0;
                player.ChangeState(State.RUNNING);
                return;
            }
        }

        //Check to slide when running
        if (playerInput.crouch && canSlide())
        {
            movement.SpeedBoost(slideBoostAmount);
            player.ChangeState(changeTo);
            slideDir = transform.forward;
            movement.characterController.height = player.crouchHeight;
            slideDownward = 0f;
            slideTime = 4f;
        }

        //Lower slidetime
        if (slideTime > 0)
        {
            if (slideDir.y < 0)
            {
                slideDownward = Mathf.Clamp(slideDownward + Time.deltaTime * Mathf.Sqrt(Mathf.Abs(slideDir.y)), 0f, 1f);
                if (slideTime <= slideBlendTime)
                    slideTime += Time.deltaTime;
            }
            else
            {
                slideDownward = Mathf.Clamp(slideDownward - Time.deltaTime, 0f, 1f);
                slideTime -= Time.deltaTime;
            }

            if (slideTime <= slideBlendTime)
            {
                
                if (player.ShouldSprint() && player.Uncrouch())
                    player.ChangeState(State.RUNNING);

                if (!player.ShouldSprint() && player.Uncrouch())
                    player.ChangeState(State.WALKING);
            }
        }
        else if (playerState == changeTo)   //FIX
        {
            if (playerInput.crouching)
            {
                player.Crouch(true);
                player.ChangeState(State.CROUCHING);
            }
            else if (!player.Uncrouch()) //Try to uncrouch, if this is false then we cannot uncrouch
            {
                player.Crouch(true); //So just keep crouched
                player.ChangeState(State.CROUCHING);
            }
        }
    }
}
