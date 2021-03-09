using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementInterface : MonoBehaviour
{
    public State changeTo;

    protected CustomCharacterController player;
    protected CharacterMovement movement;
    protected CharacterInput playerInput;
    protected State playerState;

    public virtual void Start()
    {
        player = GetComponent<CustomCharacterController>();
        player.AddMovementType(this);
        player.ChangeState(playerState);
    }

    public virtual void SetPlayerComponents(CharacterMovement move, CharacterInput input)
    { 
        movement = move; 
        playerInput = input;
    }

    public virtual void Movement()
    {
        //Movement info
    }

    public virtual void Check(bool canInteract)
    {
        //Check info
    }
}
