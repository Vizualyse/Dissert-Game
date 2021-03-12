using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRun : MovementInterface
{

    [SerializeField]
    private float minSpeed = 5f;
    [SerializeField]
    float verticalMult = 1f;

    Vector3 wallNormal = Vector3.zero;
    int wallDir = 1;

    new Camera camera;
    

    public override void Start()
    {
        camera = GetComponentInChildren<Camera>();
        base.Start();
    }

    public int getWallDir()
    {
        return wallDir;
    }

    public override void Movement()
    {
        Vector3 input = playerInput.input;
        float s = (input.y > 0) ? input.y : 0;  //removes backwards movement 

        Vector3 move = wallNormal * s;

        move.y = -camera.transform.localRotation.x * verticalMult;

        if (playerInput.Jump())
        {
            Vector3 forward = wallNormal.normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.up) * wallDir;
            Vector3 wallJump = (Vector3.up * (s + 0.5f) + forward * s * 1.5f + right * (s + 0.5f)).normalized;
            movement.Jump(wallJump, (s + 1f));
            playerInput.ResetJump();
            player.ChangeState(State.WALKING);
        }

        if (!player.hasWallToSide(wallDir) || movement.grounded || player.speed < minSpeed || !playerInput.run)
            player.ChangeState(State.WALKING);

        movement.Move(move, camera);
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract || movement.grounded || !playerInput.run || player.speed < minSpeed)
            return;

        int wall = 0;
        if (player.hasWallToSide(1))
            wall = 1;
        else if (player.hasWallToSide(-1))
            wall = -1;

        if (wall == 0) return;

        if (Physics.Raycast(transform.position + (transform.right * wall * player.info.radius), transform.right * wall, out var hit, player.info.radius))
        {
            wallDir = wall;
            wallNormal = Vector3.Cross(hit.normal, Vector3.up) * -wallDir;
            player.ChangeState(changeTo);
        }
    }


}
