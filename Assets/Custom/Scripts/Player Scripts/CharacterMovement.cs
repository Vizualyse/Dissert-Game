using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : InterpolateTransform
{
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

    [Header("Speed Adjustments")]
    public float walkSpeed = 4.0f;
    public float runSpeed = 8.0f;
    public float crouchSpeed = 2.0f;
    public float grappleSpeed = 1.75f;
    [SerializeField]
    private float accelerationFactor = .55f;
    [SerializeField]
    private float speedDecayMult = 3f;      //how much faster slowdown happens
    [SerializeField]
    public float maxSlideBoostSpeed = 14f;     //max boost speed you can get from sliding
    [SerializeField]
    private float maxSlideSpeed = 17f;          //max speed you can get by sliding downhill
    [SerializeField]
    private float slideAccel = 0.4f;
    [SerializeField]
    private float wallAccel = 3f;
    [SerializeField]
    private float minColSlowAngle = 0.5f;

    public float wallJumpDistance = 0.5f;

    [Header("Game Objets")]
    public CharacterController characterController;
    CustomCharacterController controller;

    [Header("Debug")]
    [SerializeField]
    private float currentMoveSpeed = 3f;
    [SerializeField]
    private float targetMoveSpeed = 3f;

    private float YPos = 0f;
    private float lastYPos = 0f;

    public float cumulativeGravity = 0f;

    // Start is called before the first frame update
    public override void OnEnable()
    {
        base.OnEnable();
        characterController = GetComponent<CharacterController>();
        controller = GetComponent<CustomCharacterController>();
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

        if(characterController.enabled)
            characterController.Move(adjust);
    }

    //defualt moving + air movement
    public void Move(Vector2 input, bool sprint, bool crouching)
    {   
        if (grounded)
        {
            updateMoveSpeed(sprint, crouching);     //only accelerate or decelerate when grounded
            moveDirection = new Vector3(input.x, -antiBumpFactor, input.y);      
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
            if (controller.hasObjectInfront(wallJumpDistance, out bool wall) && wall)   //for the wall jump
                UpdateJump();
        }
        //add gravity
        moveDirection.y -= gravity * Time.deltaTime;
        //move + update grounded state
        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    //slide movement
    public void Move(Vector3 direction)
    {
        updateSlideSpeed();
        //add a burst of speed after sliding
        currentMoveSpeed = targetMoveSpeed;

        Vector3 move = direction * currentMoveSpeed; 
        moveDirection.x = move.x; 
        moveDirection.y -= gravity * Time.deltaTime;
        moveDirection.z = move.z;

        UpdateJump();
        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

        if (controller.speed < 2f) targetMoveSpeed = crouchSpeed; //if sliding too slow stop player
    }

    //wallrun movement
    public void Move(Vector3 direction, Camera camera)
    {
        if(camera.transform.localRotation.x < 0) //going upwards
            targetMoveSpeed += camera.transform.localRotation.x * speedDecayMult * wallAccel * Time.deltaTime;
        else
            targetMoveSpeed += camera.transform.localRotation.x * wallAccel * Time.deltaTime;

        currentMoveSpeed = targetMoveSpeed;
        moveDirection = direction * (currentMoveSpeed + 2f);
        UpdateJump();

        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;

    }
    //dismount move
    public void Move(Vector2 input, float yVector)
    {
        Vector3 move = new Vector3(input.x, 0, input.y) * currentMoveSpeed * grappleSpeed;
        move.y = yVector;

        moveDirection.x = move.x;
        moveDirection.z = move.z;

        
        /*
        if(characterController.velocity.y > 0 && characterController.velocity.y < 1f) //if the player collides with something vertically they should lose upwards momentum
        {
            Debug.Log("true");
            moveDirection.y = 0f;
        }*/

        moveDirection.y = moveDirection.y + move.y - gravity * Time.deltaTime;

        //move + update grounded state
        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
        if (grounded)
            cumulativeGravity = 0f;
    }
    //vault + grapple
    public void Move(Vector3 direction, bool slow)
    {
        if (slow)
        { 
            targetMoveSpeed = runSpeed;
            currentMoveSpeed = targetMoveSpeed;
        }
        else
        {
            direction.x *= grappleSpeed;      //apply grapple speed increase
            direction.z *= grappleSpeed;
        }

        updateMoveSpeed(true, false);
        moveDirection = direction * currentMoveSpeed;
        //Debug.Log(moveDirection.y);

        grounded = (characterController.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    public void updateMoveSpeed(bool sprint, bool crouching)
    {
        targetMoveSpeed = sprint ? Mathf.Max(targetMoveSpeed, runSpeed) : walkSpeed;    //being in sprint mode should only speed the player up, the player can slow down if walking tho
        if (crouching) targetMoveSpeed = crouchSpeed;
        if (controller.speed < 2f) targetMoveSpeed = crouchSpeed; //if not moving try decelerate to crouch speed

        var accelAmount = (targetMoveSpeed - currentMoveSpeed) * (accelerationFactor * Time.deltaTime);
        if (accelAmount > 0)
            currentMoveSpeed += accelAmount;   //apply acceleration curve
        else
            currentMoveSpeed += accelAmount * speedDecayMult; //slow down faster than accelerate
    }

    public void updateSlideSpeed()
    {
        YPos = this.transform.position.y;
        bool evenGround = false;

        if (lastYPos == 0) lastYPos = YPos;     //if lastYPos hasn't been initialised, in practice will never be exactly 0
        if (Mathf.Abs(YPos - lastYPos) < 0.01f) evenGround = true;   //if y value has barely changed only slow down


        if (evenGround)
        {
            targetMoveSpeed -= slideAccel * Time.deltaTime;
            targetMoveSpeed = Mathf.Min(targetMoveSpeed, maxSlideSpeed);
            //Debug.Log("flat");
        }
        else
        {
            if (YPos > lastYPos)    //moving uphill
            {
                targetMoveSpeed -= slideAccel * speedDecayMult * Time.deltaTime;
                targetMoveSpeed = Mathf.Min(targetMoveSpeed, maxSlideSpeed);
                //Debug.Log("uphill");
            }
            else  //moving downhill 
            {
                targetMoveSpeed += slideAccel * Time.deltaTime;
                targetMoveSpeed = Mathf.Min(targetMoveSpeed, maxSlideSpeed);
                //Debug.Log("downhill");
            }
        }

        lastYPos = YPos;
    }

    public void SpeedBoost(float boostAmount)
    {
        if(targetMoveSpeed < maxSlideBoostSpeed)
            targetMoveSpeed = Mathf.Min(maxSlideBoostSpeed, currentMoveSpeed + boostAmount);
        
    }

    public void CollisionSlow(float percentage)
    {
        if(percentage < minColSlowAngle)
        {
            percentage = 0;
        }
        targetMoveSpeed -= (targetMoveSpeed - walkSpeed) * percentage;
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
