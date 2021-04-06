using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleMove : MovementInterface
{
    LineRenderer leftRenderer, rightRenderer;

    public Transform leftGrapple, rightGrapple, cam, playerTransform;
    bool leftActive, rightActive = true;
    public GameObject hook;
    GameObject leftHook, rightHook;
    float hookSpeed = 60f;

    Vector3 move = Vector3.zero;

    RaycastHit hit = new RaycastHit();
    Vector3 leftGrapplePoint, rightGrapplePoint = Vector3.zero;
    float leftGrappleLength, rightGrappleLength = 0f;
    float leftGrappleBreakSmooth, rightGrappleBreakSmooth = 0f;

    int layerMask;

    [SerializeField]
    float maxDifference = 3.5f;
    [SerializeField]
    float grapplePower = 5f;
    [SerializeField]
    float maxDistance = 100f;
    [SerializeField]
    float verticalSpeed = 3f;

    public override void Start()
    {
        layerMask = ~LayerMask.GetMask("Player"); 

        leftRenderer = leftGrapple.gameObject.GetComponent<LineRenderer>();
        rightRenderer = rightGrapple.gameObject.GetComponent<LineRenderer>();
        leftRenderer.enabled = false;
        rightRenderer.enabled = false;
        base.Start();
    }

    private void EndGrapple(int grappleSide, bool smooth)
    {
        if (grappleSide == 0)
        {
            if(smooth && leftGrappleBreakSmooth < 3)
            {
                leftGrappleBreakSmooth++;
                return;
            }
            rightGrappleBreakSmooth = 0f;
            leftActive = false;
            leftRenderer.enabled = false;
            Destroy(leftHook);
            leftHook = null;
        }
        else
        {
            if (smooth && leftGrappleBreakSmooth < 3)
            {
                rightGrappleBreakSmooth++;
                return;
            }
            rightGrappleBreakSmooth = 0f;
            rightActive = false;
            rightRenderer.enabled = false;
            Destroy(rightHook);
            rightHook = null;
        }

        if(!leftActive && !rightActive)
        {
            move.y = 0;
            GetComponent<DismountingMove>().setTrajectory();
            player.ChangeState(State.DISMOUNTING);
        }
    }


    public override void Movement()
    {/*
        if (Input.GetMouseButton(0))
        {
            if (!leftActive && Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, layerMask))
            {
                leftActive = true;
                player.ChangeState(changeTo);
                leftRenderer.enabled = true;
                leftGrapplePoint = hit.point;
                leftGrappleLength = hit.distance;
            }
        }
        else
            EndGrapple(0);
        if (Input.GetMouseButton(1))
        {
            if (!rightActive && Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, layerMask))
            {
                rightActive = true;
                rightRenderer.enabled = true;
                rightGrapplePoint = hit.point;
                rightGrappleLength = hit.distance;
            }
        }
        else
            EndGrapple(1);
        */
        if (!Input.GetMouseButton(0))
            EndGrapple(0, false);
        if (!Input.GetMouseButton(1))
            EndGrapple(1, false);

        if (leftActive || rightActive)
            player.ChangeState(changeTo);
        else
            return;

        if (player.state == changeTo)
        {
            move.x = 0; move.z = 0;

            Vector3 input = Vector3.zero;
            //horizontal input
            input.x += playerInput.input.x;
            input.z += playerInput.input.y >= 0 ? playerInput.input.y : playerInput.input.y * 0.10f;

            input = playerTransform.TransformDirection(input);
            move += input;

            //apply grapple (0 for left 1 for right)
            Vector3 grappleReturn = Vector3.zero;
            if (leftActive)
                grappleReturn = singleGrapple(grappleReturn, 0);
            if(rightActive)
                grappleReturn = singleGrapple(grappleReturn, 1);

            grappleReturn.x = grappleReturn.x > 0.2f ? grappleReturn.x : 0f;    //without this it jitters
            grappleReturn.z = grappleReturn.z > 0.2f ? grappleReturn.z : 0f;
            grappleReturn.y = 0;
            move += grappleReturn;

            move = Vector3.ClampMagnitude(move, 1.3f);    //clamp horizontal move now

            //vertical input
            if (Input.GetButton("Jump"))
                move.y += verticalSpeed * Time.deltaTime;
            else
                move.y += -verticalSpeed * Time.deltaTime;

            movement.Move(move, false);
        }
    }

    public Vector3 singleGrapple(Vector3 move, int grappleSide)
    {
        Transform grapple; Vector3 grapplePoint; float grappleLength;
        if (grappleSide == 0)
        {
            grapple = leftGrapple; grapplePoint = leftGrapplePoint; grappleLength = leftGrappleLength;
        }
        else
        {
            grapple = rightGrapple; grapplePoint = rightGrapplePoint; grappleLength = rightGrappleLength;
        }

        //tether input
        RaycastHit check;
        Vector3 grappleDireciton = (grapplePoint - grapple.position).normalized;
        Physics.Raycast(grapple.position, grappleDireciton, out check, maxDistance, layerMask);
        float difference = check.distance - grappleLength;
        if (difference > maxDifference || difference < -maxDifference)
        {
            //try { Debug.Log("Grapple too long " + check.distance + " vs " + grappleLength + " (collided with " + check.collider.name + ")"); } catch { }
            EndGrapple(grappleSide, true);
            return Vector3.zero;
        }
        else if (difference > 0)    //greater than zero means the player is trying to move away from the grapple
        {
            float pullStrength = grapplePower * (difference / maxDifference);
            move += grappleDireciton * pullStrength;
        }
        else
            if(grappleSide == 0)
                leftGrappleLength = check.distance;
            else
                rightGrappleLength = check.distance;

        //reel
        Vector3 horizontalPullDir = new Vector3((grapplePoint - grapple.position).x, 0, (grapplePoint - grapple.position).z);
        //move += horizontalPullDir.normalized * .45f;

        return move;
    }

    private GameObject shootGrapple(int grappleSide)
    {
        Vector3 grapplePosition;
        if (grappleSide == 0)
            grapplePosition = leftGrapple.position;
        else
            grapplePosition = rightGrapple.position;

        GameObject hookInstance = Instantiate(hook, grapplePosition, Quaternion.identity);
        Physics.Raycast(playerTransform.position, cam.forward, out hit, 100, layerMask);
        hookInstance.GetComponent<Rigidbody>().AddForce((hit.point - grapplePosition).normalized * hookSpeed, ForceMode.Impulse);
        GrappleHook hookScript = hookInstance.GetComponent<GrappleHook>();
        hookScript.grappleSide = grappleSide; hookScript.grappleScript = this;

        if (grappleSide == 0)
        {
            leftRenderer.enabled = true;
            hookScript.rend = leftRenderer;
            hookScript.grapple = leftGrapple;
        }
        else
        {
            rightRenderer.enabled = true;
            hookScript.rend = rightRenderer;
            hookScript.grapple = rightGrapple;
        }
            

        return hookInstance;
    }

    public void attachGrapple(int grappleSide, Vector3 point)
    {
        if (grappleSide == 0)
        {
            leftActive = true;
            player.ChangeState(changeTo);
            leftGrapplePoint = point;
            leftGrappleLength = Vector3.Distance(point, leftGrapple.position);
        }
        else
        {
            rightActive = true;
            player.ChangeState(changeTo);
            rightGrapplePoint = point;
            rightGrappleLength = Vector3.Distance(point, rightGrapple.position);
        }
        Movement();
    }

    private void Update()
    {
        Check(true);
    }

    private void LateUpdate()
    {
        leftRenderer.SetPositions(new Vector3[] { leftGrapplePoint, leftGrapple.position });
        rightRenderer.SetPositions(new Vector3[] { rightGrapplePoint, rightGrapple.position });
    }

    public override void Check(bool canInteract)
    {
        if (!canInteract) return;

        if (leftHook == null && Input.GetMouseButtonDown(0))
            leftHook = shootGrapple(0);

        if (rightHook == null && Input.GetMouseButtonDown(1))
            rightHook = shootGrapple(1);
        /*
        if (Input.GetMouseButtonDown(0)  || Input.GetMouseButtonDown(1))
            Movement();
        */
    }
}
