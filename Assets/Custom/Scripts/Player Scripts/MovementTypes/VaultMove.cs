using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VaultMove : MovementInterface
{

    Vector3 vaultOver;
    Vector3 vaultDir;

    GameObject vaultHelper;
    float duration = 0;
    float maxDuration = 0.75f;

    void CreateVaultHelper()
    {
        vaultHelper = new GameObject();
        vaultHelper.transform.name = "_Vault Helper";
    }
    void SetVaultHelper()
    {
        vaultHelper.transform.position = vaultOver;
        vaultHelper.transform.rotation = Quaternion.LookRotation(vaultDir);
    }
    public override void SetPlayerComponents(CharacterMovement move, CharacterInput input)
    {
        base.SetPlayerComponents(move, input);
        CreateVaultHelper();
    }

    public void Dismount()
    {
        if (player.state == changeTo)
        {
            duration += Time.deltaTime;
            if (duration > maxDuration)
            {
                duration = 0;
                player.ChangeState(State.DISMOUNTING);
            }
        }
        else
            duration = 0;
    }

    public override void Movement()
    {
        Dismount();     //forces the player off if they've been vaulting too long

        Vector3 dir = vaultOver - transform.position;
        Vector3 localPos = vaultHelper.transform.InverseTransformPoint(transform.position);
        Vector3 move = (vaultDir + (Vector3.up * -(localPos.z - player.info.radius) * player.info.height)).normalized;

        if (localPos.z < -(player.info.radius * 2f))
            move = dir.normalized;
        else if (localPos.z > player.info.height)
        {
            player.ChangeState(State.DISMOUNTING);
        }

        movement.Move(move, true);
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract) return;
        if (player.state == changeTo) return;
        if (player.state == State.SLIDING) return;
        if (playerInput.input.y < 0) return; //no vault if the player is trying to move backwards
        
        float movementAdjust = (Vector3.ClampMagnitude(movement.characterController.velocity, 16f).magnitude / 16f);
        float checkDis = player.info.radius + movementAdjust/2;

        if (player.hasObjectInfront(checkDis))
        {
            if (Physics.SphereCast(transform.position + (transform.forward * (player.info.radius - 0.25f)), 0.25f, transform.forward, out var sphereHit, checkDis))
            {
                if (Physics.SphereCast(sphereHit.point + (Vector3.up * player.info.halfheight), player.info.radius, Vector3.down, out var hit, player.info.halfheight - player.info.radius))
                {
                    Debug.DrawRay(hit.point + (Vector3.up * player.info.radius), Vector3.up * player.info.halfheight);
                    //Check above the point to make sure the player can fit
                    if (Physics.SphereCast(hit.point + (Vector3.up * player.info.radius), player.info.radius, Vector3.up, out var trash, player.info.halfheight))
                        return; //If cannot fit the player then do not vault

                    //Check in-front of the vault to see if something is blocking
                    Vector3 fromPlayer = transform.position;
                    Vector3 toVault = hit.point + (Vector3.up * player.info.radius);
                    fromPlayer.y = toVault.y;

                    Vector3 dir = (toVault - fromPlayer);
                    if (Physics.SphereCast(fromPlayer, player.info.radius / 2f, dir.normalized, out var trash2, dir.magnitude + player.info.radius))
                        return; //If we hit something blocking the vault, then do nothing

                    vaultOver = hit.point;
                    vaultDir = transform.forward;
                    SetVaultHelper();

                    player.ChangeState(changeTo);
                }
            }
        }
    }


}
