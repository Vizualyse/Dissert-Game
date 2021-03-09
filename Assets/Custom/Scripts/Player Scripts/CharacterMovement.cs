using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : InterpolateTransform
{
    public float walkSpeed = 4.0f;
    public float runSpeed = 8.0f;
    public float crouchSpeed = 2.0f;
    [SerializeField]
    private float gravity = 20.0f;
    [SerializeField]
    private float jumpSpeed = 8.0f;

    [SerializeField]
    private float antiBumpFactor = .75f;
    
    [HideInInspector]
    public Vector3 moveDirection = Vector3.zero;

    public bool grounded = false;
    public Vector3 jump = Vector3.zero;
    private float jumpPower;
    Vector3 jumpedDir;

    [SerializeField]
    private float currentMoveSpeed = 3f;
    [SerializeField]
    private float targetMoveSpeed = 3f;
    [SerializeField]
    private float accelerationFactor = .55f;
    [SerializeField]
    private float speedDecayMult = 3f;      //how much faster slowdown happens
    [SerializeField]
    private float maxSlideSpeed = 14f;     

    public CharacterController characterController;
    UnityEvent onReset = new UnityEvent();

    public void AddToReset(UnityAction call)
    {
        onReset.AddListener(call);
    }

    public override void ResetPositionTo(Vector3 resetTo)
    {
        characterController.enabled = false;
        StartCoroutine(forcePosition());
        IEnumerator forcePosition()
        {
            //Reset position to 'resetTo'
            transform.position = resetTo;
            //Remove old interpolation
            ForgetPreviousTransforms();
            yield return new WaitForEndOfFrame();
        }
        characterController.enabled = true;
        onReset.Invoke();
    }

    // Start is called before the first frame update
    public override void OnEnable()
    {
        base.OnEnable();
        characterController = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public override void Update()
    {
        Vector3 newestTransform = m_lastPositions[m_newTransformIndex];
        Vector3 olderTransform = m_lastPositions[OldTransformIndex()];

        Vector3 adjust = Vector3.Lerp(olderTransform, newestTransform, InterpolationControl.InterpolationFactor);
        adjust -= transform.position;

        characterController.Move(adjust);
    }

    public override void FixedUpdate()
    {
        
    }

    public void Move(Vector2 input, bool sprint, bool crouching)
    {
        targetMoveSpeed = sprint ? Mathf.Max(targetMoveSpeed, runSpeed) : walkSpeed;    //being in sprint mode should only speed the player up, the player can slow down if walking tho
        if (crouching) targetMoveSpeed = crouchSpeed;
        if (characterController.velocity.magnitude < 0.001f) targetMoveSpeed = crouchSpeed; //if not moving try decelerate to crouch speed

        var accelAmount = (targetMoveSpeed - currentMoveSpeed) * (accelerationFactor * Time.deltaTime);
        if (accelAmount > 0)
            currentMoveSpeed += accelAmount;   //apply acceleration curve
        else
            currentMoveSpeed += accelAmount* speedDecayMult; //slow down faster than accelerate

        if (grounded)
        {
            moveDirection = new Vector3(input.x, -antiBumpFactor, input.y);      //anti bump factor stops the player slowing down when they hit ledges
            moveDirection = transform.TransformDirection(moveDirection) * currentMoveSpeed;
            UpdateJump();
        }
        else           //in air controls
        {
            Vector3 adjust = new Vector3(input.x, 0, input.y);
            adjust = transform.TransformDirection(adjust);
            jumpedDir += adjust * Time.fixedDeltaTime * jumpPower * 2f;
            jumpedDir = Vector3.ClampMagnitude(jumpedDir, jumpPower);
            moveDirection.x = jumpedDir.x;
            moveDirection.z = jumpedDir.z;
        }

        //add gravity
        moveDirection.y -= gravity * Time.deltaTime;
        //move + update grounded state
        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    public void Move(Vector3 direction, float appliedGravity, float setY)
    {
        //add a burst of speed after sliding
        currentMoveSpeed = targetMoveSpeed;

        Vector3 move = direction * currentMoveSpeed; 
        if (appliedGravity > 0)
        {
            moveDirection.x = move.x;
            if (setY != 0) moveDirection.y = setY * currentMoveSpeed;
            moveDirection.y -= gravity * Time.deltaTime * appliedGravity;
            moveDirection.z = move.z;
        }
        else
            moveDirection = move;

        UpdateJump();

        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    public void SpeedBoost(float boostAmount)
    {
        targetMoveSpeed = Mathf.Min(maxSlideSpeed, currentMoveSpeed + boostAmount);
    }

    public void CollisionSlow()
    {
        targetMoveSpeed = walkSpeed;
        currentMoveSpeed = targetMoveSpeed;
    }

    public void Jump(Vector3 dir, float mult)
    {
        jump = dir * mult;
    }

    public void UpdateJump()
    {
        if (jump != Vector3.zero)
        {
            Vector3 dir = (jump * jumpSpeed);
            if (dir.x != 0) moveDirection.x = dir.x;
            if (dir.y != 0) moveDirection.y = dir.y;
            if (dir.z != 0) moveDirection.z = dir.z;

            Vector3 move = moveDirection;
            jumpedDir = move; move.y = 0;
            jumpPower = Mathf.Min(move.magnitude, jumpSpeed);
            jumpPower = Mathf.Max(jumpPower, currentMoveSpeed);
        }
        else
            jumpedDir = Vector3.zero;
        jump = Vector3.zero;
    }

    public float GetCurrentMovementSpeed()
    {
        return currentMoveSpeed;
    }
    
}
