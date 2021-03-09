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
    private float currentMoveSpeed = 0f;

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

        currentMoveSpeed = sprint ? runSpeed : walkSpeed;        //if sprinting use run speed
        if (crouching) currentMoveSpeed = crouchSpeed;

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

    
}
