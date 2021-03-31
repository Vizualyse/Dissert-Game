using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DismountingMove : MovementInterface
{
    Vector2 trajectory = Vector2.zero;
    float yVector = 0f;

    public void setTrajectory()         //used by grapple
    {
        trajectory = new Vector2(player.getTrajectory.x, player.getTrajectory.z);
        yVector = player.getTrajectory.y;
    }

    public override void Movement()
    {
        try{GetComponent<GrappleMove>().Check(true);} catch { Debug.Log("Grapple Component not found"); }

        if (player.hasWallToSide(-1) || player.hasWallToSide(1))
        {
            player.ChangeState(State.WALLRUN);
            trajectory = Vector2.zero;
            return;
        }
        else if (player.state == changeTo && movement.grounded)
        { 
            player.ChangeState(State.WALKING);
            trajectory = Vector2.zero;
            return;
        }
        else if (trajectory == Vector2.zero)
        {
            trajectory = new Vector2(player.getTrajectory.x, player.getTrajectory.z);
        }

        //movement.Move(playerInput.input);
        movement.Move(trajectory, yVector);
        yVector = 0f;
    }
}
