using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleMove : MovementInterface
{
    LineRenderer rightRenderer;

    public Transform rightGrapple, cam, playerTransform;

    Vector3 move = Vector3.zero;

    RaycastHit hit = new RaycastHit();
    Vector3 grapplePoint = Vector3.zero;
    float grappleLength = 0f;
    
    [SerializeField]
    float maxDifference = 1.5f;
    [SerializeField]
    float grapplePower = 5f;
    [SerializeField]
    float maxDistance = 100f;
    [SerializeField]
    float verticalSpeed = 3f;

    public override void Start()
    {
        rightRenderer = rightGrapple.gameObject.GetComponent<LineRenderer>();
        rightRenderer.enabled = false;
        base.Start();
    }

    private void Update()
    {
        
    }

    private void EndGrapple()
    {
        move.y = 0f;
        rightRenderer.enabled = false;
        GetComponent<DismountingMove>().setTrajectory();
        player.ChangeState(State.DISMOUNTING);
    }

    public override void Movement()
    {
        if (!Input.GetMouseButton(0))
        {
            EndGrapple();
            return;
        }
        int layerMask = ~LayerMask.GetMask("Player");

        if (player.state == changeTo)
        {
            move.x = 0;
            move.z = 0;

            //vertical input
            /*
            float y = cam.localEulerAngles.x;
            if (y > 90) y -= 360;
            y /= 90;
            move.y = y * verticalSpeed;
            */
            if (Input.GetButton("Jump"))
            {
                move.y += verticalSpeed * Time.deltaTime;
            }
            else
            {
                move.y += -verticalSpeed * Time.deltaTime;
            }

            //horizontal input
            move.x = playerInput.input.x;
            move.z = playerInput.input.y;

            move = playerTransform.TransformDirection(move);

            //tether input
            RaycastHit check;
            Vector3 grappleDireciton = (grapplePoint - playerTransform.position).normalized;
            Physics.Raycast(playerTransform.position, grappleDireciton, out check, maxDistance, layerMask);
            float difference = check.distance - grappleLength;
            if (difference > maxDifference || difference < -maxDifference)
            {
                EndGrapple();
                return;
            }
            else if (difference > 0)    //greater than zero means the player is trying to move away from the grapple
            {
                float pullStrength = grapplePower * (difference / maxDifference);
                move += grappleDireciton * pullStrength;
            }
            else
            {
                //Debug.Log(check.distance + " : " + grappleLength);
                grappleLength = check.distance;
            }

            movement.Move(move, false);
        }
        else if (Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, layerMask))
        {
            player.ChangeState(changeTo);
            rightRenderer.enabled = true;
            grapplePoint = hit.point;
            grappleLength = hit.distance;
        }
    }

    private void LateUpdate()
    {
        rightRenderer.SetPositions(new Vector3[] { rightGrapple.position, grapplePoint });
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract) return;
        if (player.state == changeTo) return;

        if (Input.GetMouseButtonDown(0))
            Movement();
    }
}
