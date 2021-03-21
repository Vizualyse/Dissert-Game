using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DismountingMove : MovementInterface
{
    public override void Movement()
    {
        if (player.hasWallToSide(-1) || player.hasWallToSide(1))
        { 
            player.ChangeState(State.WALLRUN);
            return;
        }
        movement.Move(playerInput.input);

        if (player.state == changeTo && movement.grounded)
            player.ChangeState(State.WALKING);
        
    }
}
