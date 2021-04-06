using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideMove : MovementInterface
{
    public FloatRange slideSpeed = new FloatRange(7.0f, 12.0f);

    [SerializeField]
    float minSlideSpeed = 5.0f;
    [SerializeField]
    float slideBoostAmount = 3f;

    [SerializeField]
    Vector3 slideDir;

    bool slideOnLand = false;

    bool canSlide()
    {
        if (player.state == changeTo || !playerInput.run || !movement.grounded) return false;
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
            if (player.Uncrouch())
            {
                movement.Jump(slideDir + Vector3.up, 1f);
                playerInput.ResetJump();
                player.ChangeState(State.RUNNING);
            }
        }

        //movement.Move(slideDir, 1f, slideDir.y);      //locked slide
        movement.Move(transform.TransformDirection(new Vector3(playerInput.input.x * 0.1f, 0, playerInput.input.y)));
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract) return;

        //Start sliding on landing
        if (!movement.grounded && playerInput.crouch)
        {
            slideOnLand = true;
        }

        //Unslide
        if (playerInput.crouch && player.state == State.SLIDING)
        {
            if (player.Uncrouch())
            {
                movement.SpeedBoost(-slideBoostAmount);
                player.ChangeState(State.RUNNING);
                return;
            }
        }

        //Check to slide when running
        if ((playerInput.crouch && canSlide()) || (slideOnLand && canSlide()))
        {
            slideOnLand = false;

            movement.SpeedBoost(slideBoostAmount);
            player.ChangeState(changeTo);
            slideDir = transform.forward;
            movement.characterController.height = player.crouchHeight;
        }


        if (movement.GetCurrentMovementSpeed() <= minSlideSpeed && player.state == changeTo)
        {
            if (playerInput.crouching)  //if the player is trying to stay crouched, don't uncrouch
            {
                player.Crouch(true);
                player.ChangeState(State.CROUCHING);
            }
            else if (!player.Uncrouch()) //Try to uncrouch, if this is false then we cannot uncrouch
            {
                player.Crouch(true); //So just keep crouched
                player.ChangeState(State.CROUCHING);
            }
            else
            {
                player.ChangeState(State.RUNNING);
            }
        }
    }
}
