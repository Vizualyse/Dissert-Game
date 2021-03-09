using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State { IDLE, WALKING, CROUCHING, RUNNING, SLIDING }
public class CustomCharacterController : MonoBehaviour
{
    public State state;
    public LayerMask collisionLayer;
    public float crouchHeight = 1f;
    public CharInfo info;

    bool canInteract;
    CharacterInput characterInput;
    CharacterMovement characterMovement;

    List<MovementInterface> movements;

    public void ChangeState(State s)
    {
        state = s;
    }

    void Start()
    {
        characterInput = GetComponent<CharacterInput>();
        characterMovement = GetComponent<CharacterMovement>();
        characterMovement.AddToReset(() => { state = State.WALKING; });

        info = new CharInfo(characterMovement.characterController.radius, characterMovement.characterController.height);
    }

    public void AddMovementType(MovementInterface movement)
    {
        if (movements == null) movements = new List<MovementInterface>();
        movement.SetPlayerComponents(characterMovement, characterInput);
        movements.Add(movement);
    }

    void Update()
    {
        //Updates
        UpdateInteraction();
        UpdateMovingStatus();

        //Checks
        CheckCrouching();
        foreach (MovementInterface moveType in movements)
        {
            if (moveType.enabled)
                moveType.Check(canInteract);
        }

    }

    void UpdateInteraction()
    {
        if ((int)state >= 5)
            canInteract = false;
        else if (!canInteract)
        {
            if (characterMovement.grounded || characterMovement.moveDirection.y < 0)
                canInteract = true;
        }
    }

    void UpdateMovingStatus()
    {
        if ((int)state <= 1 || isSprinting())   //if idle, walking or running
        {
            if (characterInput.input.magnitude > 0.02f)
                ChangeState(ShouldSprint() ? State.RUNNING : State.WALKING);
            else
                ChangeState(State.IDLE);
        }

    }

    //this method is created so I can add a stamina bar later
    public bool ShouldSprint()
    {
        bool sprint = false;
        sprint = (characterInput.run && characterInput.input.y > 0);

        return sprint;
    }

    private void FixedUpdate()
    {
        foreach (MovementInterface moveType in movements)
        {
            if (state == moveType.changeTo)
            {
                moveType.Movement();
                return;
            }
        }

        NormalMove();
    }

    void NormalMove()
    {
        if(isSprinting() && isCrouching())
            Uncrouch();

        if(characterMovement.grounded && characterInput.Jump())
        {
            if(state == State.CROUCHING)
            {
                if (!Uncrouch())
                    return; //if you cant uncrouch abort jump attempt
            }

            characterMovement.Jump(Vector3.up, 1f);
            characterInput.ResetJump();
        }

        characterMovement.Move(characterInput.input, alreadySprinting(), isCrouching());

    }

    public bool isSprinting()
    {
        return (state == State.RUNNING && characterMovement.grounded);
    }

    public bool alreadySprinting()
    {
        return (state == State.RUNNING);
    }

    public bool isCrouching()
    {
        return (state == State.CROUCHING);
    }

    void CheckCrouching()
    {
        if (!characterMovement.grounded || (int)state > 2) return;  //not grounded or if idle, walking or crouching

        if(characterInput.run)
        {   
            Uncrouch();
            return;
        }

        if(characterInput.crouch)
        {
            if (state != State.CROUCHING)
                Crouch(true);
            else
                Uncrouch();
        }
    }

    public void Crouch(bool setStatus)
    {
        characterMovement.characterController.height = crouchHeight;
        if (setStatus) ChangeState(State.CROUCHING);
    }

    public bool Uncrouch()
    {
        Vector3 bottom = transform.position - (Vector3.up * ((crouchHeight / 2) - info.radius));
        //check if the character can stand up
        bool isBlocked = Physics.SphereCast(bottom, info.radius, Vector3.up, out var hit, info.height - info.radius, collisionLayer);
        if (isBlocked) return false;
        characterMovement.characterController.height = info.height;
        ChangeState(State.WALKING);
        return true;
    }
}


public class CharInfo
{
    public float rayDistance;
    public float radius;
    public float height;
    public float halfradius;
    public float halfheight;

    public CharInfo(float r, float h)
    {
        radius = r; height = h;
        halfradius = r / 2f; halfheight = h / 2f;
        rayDistance = halfheight + radius + .175f;
    }
}