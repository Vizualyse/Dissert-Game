using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State { IDLE, WALKING, CROUCHING, RUNNING, SLIDING, WALLRUN, DISMOUNTING, VAULTING, GRAPPLING}
public class CustomCharacterController : MonoBehaviour
{
    public State state;
    public float crouchHeight = 1f;
    public CharInfo info;

    bool canInteract;
    CharacterInput characterInput;
    CharacterMovement characterMovement;

    List<MovementInterface> movements;

    Vector3 lastPos;
    float averageSpeed = 0f;
    float speedSmoothingFactor = 20f;
    float speedCounter = 0;
    public float speed = 0f;

    CollisionDetection collisionDetection;
    public bool debugDraw = false;


    public void ChangeState(State s)
    {
        state = s;
    }

    void Start()
    {
        characterInput = GetComponent<CharacterInput>();
        characterMovement = GetComponent<CharacterMovement>();

        lastPos = this.transform.position;
        collisionDetection = GetComponentInChildren<CollisionDetection>();

        info = new CharInfo(characterMovement.characterController.radius, characterMovement.characterController.height);
    }

    public void AddMovementType(MovementInterface movement)
    {
        if (movements == null) movements = new List<MovementInterface>();
        movement.SetPlayerComponents(characterMovement, characterInput);
        movements.Add(movement);
    }

    void Update()
    {
        //Updates
        UpdateInteraction();
        UpdateMovingStatus();

        //Checks
        CheckCrouching();
        foreach (MovementInterface moveType in movements)
        {
            if (moveType.enabled)
                moveType.Check(canInteract);
        }

    }

    void UpdateInteraction()
    {
        if ((int)state >= 5)
            canInteract = false;
        else if (!canInteract)
        {
            if (characterMovement.grounded || characterMovement.moveDirection.y < 0)
                canInteract = true;
        }
    }

    void UpdateMovingStatus()
    {
        if ((int)state <= 1 || isSprinting())   //if idle, walking or running
        {
            if (characterInput.input.magnitude > 0.02f)
                ChangeState(ShouldSprint() ? State.RUNNING : State.WALKING);
            else
                ChangeState(State.IDLE);
        }

    }

    //this method is created so I can add a stamina bar later
    public bool ShouldSprint()
    {
        bool sprint = false;
        sprint = (characterInput.run && characterInput.input.y > 0);

        return sprint;
    }

    private void FixedUpdate()
    {
        UpdateSpeed();

        if (state == State.VAULTING)
            collisionDetection.active = false;
        else
            collisionDetection.active = true;

        foreach (MovementInterface moveType in movements)
        {
            if (state == moveType.changeTo)
            {
                moveType.Movement();
                return;
            }
        }

        NormalMove();
    }

    void NormalMove()
    {
        if(isSprinting() && isCrouching())
            Uncrouch();

        if(characterInput.Jump())
        {
            if (characterMovement.grounded)
            {
                if (state == State.CROUCHING)
                {
                    if (!Uncrouch())
                        return; //if you cant uncrouch abort jump attempt
                }
                characterMovement.Jump(Vector3.up, 1f);
                characterInput.ResetJump();
            }
            else if (hasObjectInfront(characterMovement.wallJumpDistance, out bool flat) && flat)
            {
                if (state == State.CROUCHING)
                {
                    if (!Uncrouch())
                        return;
                }
                
                Vector3 trajectory = getTrajectory;
                trajectory = -(trajectory) + transform.position;     //inverts the trajectory
                trajectory = Vector3.ClampMagnitude(trajectory, 1f); trajectory.y = 0;
                trajectory = -2 * transform.forward;

                characterMovement.Jump(Vector3.up + trajectory, 1f);
                characterInput.ResetJump();
            }
        }
        
        
        
        
        characterMovement.Move(characterInput.input, alreadySprinting(), isCrouching());

    }

    public bool isSprinting()
    {
        return (state == State.RUNNING && characterMovement.grounded);
    }

    public bool alreadySprinting()
    {
        return (state == State.RUNNING);
    }

    public bool isCrouching()
    {
        return (state == State.CROUCHING);
    }

    public Vector3 getTrajectory
    {  get { return collisionDetection.trajectory - transform.position; } }

    void CheckCrouching()
    {
        if (!characterMovement.grounded || (int)state > 2) return;  //not grounded or if idle, walking or crouching

        if(characterInput.run)
        {   
            Uncrouch();
            return;
        }

        if(characterInput.crouch)
        {
            if (state != State.CROUCHING)
                Crouch(true);
            else
                Uncrouch();
        }
    }

    public void Crouch(bool setStatus)
    {
        characterMovement.characterController.height = crouchHeight;
        if (setStatus) ChangeState(State.CROUCHING);
    }

    public bool Uncrouch()
    {
        Vector3 bottom = transform.position - (Vector3.up * ((crouchHeight / 2) - info.radius));
        //check if the character can stand up
        bool isBlocked = Physics.SphereCast(bottom, info.radius, Vector3.up, out var hit, info.height - info.radius);
        if (isBlocked) return false;
        characterMovement.characterController.height = info.height;
        ChangeState(State.WALKING);
        return true;
    }

    public void UpdateSpeed()
    {
        Vector3 pos = this.transform.position;
        Vector3 moved = pos - lastPos;
        moved = new Vector3(moved.x, 0, moved.z);
        lastPos = pos;
        averageSpeed += moved.magnitude / Time.deltaTime;

        if (speedCounter == speedSmoothingFactor) //makes the counter fluctuate less
        {
            speed = averageSpeed / speedSmoothingFactor;
            speedCounter = 0;
            averageSpeed = 0;
        }
        else
            speedCounter++;
    }

    public bool hasObjectInfront(float distance)
    {
        return hasObjectInfront(distance, out bool ignore);
    }

    public bool hasObjectInfront(float distance, out bool flatWall)
    {
        Vector3 top = transform.position + (transform.forward * 0.25f);
        Vector3 bottom = top - (transform.up * info.height);
        RaycastHit[] raycasts = Physics.CapsuleCastAll(top, bottom, 0.25f, transform.forward, distance);
        float playerMidY = transform.position.y + info.height * 0.125f;

        int layermask = ~LayerMask.GetMask("Player");//everything but the player

        Physics.Raycast(transform.position, -transform.up, out RaycastHit floorRay, 5f, layermask);
        Physics.Raycast(transform.position, transform.forward, out RaycastHit wallRay, 5f, layermask);
        try 
        {
            if (floorRay.collider != null)
                flatWall = !floorRay.collider.name.Equals(wallRay.collider.name);
            else if (wallRay.collider == null)
                flatWall = false;
            else
                flatWall = true;
        }
        catch
        {
            flatWall = false;
        }

        foreach (RaycastHit ray in raycasts)
        {
            if (ray.transform.name != "Player" && ray.transform.name != "Collider")
            {
                float y = ray.collider.bounds.max.y;
                if (y > playerMidY)
                    return true;
            }
        }

        return false;
    }

    private void getWallContactPoints(int dir, out Vector3 side, out Vector3 topForward, out Vector3 topBackward, out Vector3 bottomForward, out Vector3 bottomBackward)
    {
        side = transform.position + (transform.right * info.radius * dir);
        Vector3 top = side + (transform.up * info.halfheight);
        topForward = top + (transform.forward * info.radius);
        topBackward = top - (transform.forward * info.radius);
        Vector3 bottom = side - (transform.up * info.halfheight);
        bottomForward = bottom + (transform.forward * info.radius);
        bottomBackward = bottom - (transform.forward * info.radius);
    }

    public bool hasWallToSide(int dir)
    {
        getWallContactPoints(dir, out Vector3 side, out Vector3 topForward, out Vector3 topBackward, out Vector3 bottomForward, out Vector3 bottomBackward);

        List<RaycastHit> raycasts = new List<RaycastHit>();
        List<RaycastHit> temp = new List<RaycastHit>();

        //to reduce computation the middle ray cast is tested alone first
        foreach (RaycastHit ray in raysWithoutPlayer(Physics.RaycastAll(side, transform.right * dir, 0.6f)))
            raycasts.Add(ray);

        if (raycasts.Count == 0) return false;  //middle raycast not hit, not valid wall

        temp.AddRange(Physics.RaycastAll(topForward, transform.right * dir, 0.4f));
        if(temp.Count >= 1) //collision in the top front
        {
            int tempCount = temp.Count;
            temp.AddRange(raysWithoutPlayer(Physics.RaycastAll(bottomForward, transform.right * dir, 0.4f)));
            temp.AddRange(raysWithoutPlayer(Physics.RaycastAll(bottomBackward, transform.right * dir, 0.4f)));
            if(temp.Count > tempCount)      //if at least 1 more raycast is added add it to the list
                raycasts.AddRange(temp);
        }
        temp.Clear(); 
        temp.AddRange(Physics.RaycastAll(topBackward, transform.right * dir, 0.4f));
        if (raycasts.Count < 3 && temp.Count >= 1) //collision in the top back, if front already has 3 raycasts skip this
        {
            int tempCount = temp.Count;
            temp.AddRange(raysWithoutPlayer(Physics.RaycastAll(bottomForward, transform.right * dir, 0.4f)));
            temp.AddRange(raysWithoutPlayer(Physics.RaycastAll(bottomBackward, transform.right * dir, 0.4f)));
            if (temp.Count > tempCount)     //if at least 1 more raycast is added add it to the list
                raycasts.AddRange(temp);
        }

        temp.Clear();
        int hits = 0;
        while(hits < 3 && raycasts.Count > 0)
        {
            hits = 0;
            RaycastHit ray = raycasts[0];
            temp.Add(ray);
            GameObject wall = ray.collider.gameObject;
            foreach(RaycastHit nextRay in raycasts)
            {
                if (wall.Equals(nextRay.collider.gameObject))
                {
                    hits++;
                    temp.Add(nextRay);
                }
            }
            foreach (RaycastHit remove in temp) raycasts.Remove(remove);
        }

        return hits >= 3;
    }

    private List<RaycastHit> raysWithoutPlayer(RaycastHit[] raycasts)
    {
        List<RaycastHit> rays = new List<RaycastHit>();
        foreach (RaycastHit ray in raycasts)
        {
            Transform transform = ray.transform;
            while (transform.parent != null)
                transform = transform.parent;
            if (!transform.name.Equals(this.name))  //if hit anything other than the player
                rays.Add(ray);
        }

        return rays;
    }

    //used to test the contact points for wall running
    private void OnDrawGizmos()
    {
        if (debugDraw)
        {
            int dir = 1;    //1 for right -1 for left
            getWallContactPoints(dir, out Vector3 side, out Vector3 topForward, out Vector3 topBackward, out Vector3 bottomForward, out Vector3 bottomBackward);
            Gizmos.DrawCube(side, Vector3.one * 0.1f);
            Gizmos.DrawCube(topForward, Vector3.one * 0.1f);
            Gizmos.DrawCube(topBackward, Vector3.one * 0.1f);
            Gizmos.DrawCube(bottomForward, Vector3.one * 0.1f);
            Gizmos.DrawCube(bottomBackward, Vector3.one * 0.1f);
        }

    }
}


public class CharInfo
{
    public float rayDistance;
    public float radius;
    public float height;
    public float halfradius;
    public float halfheight;

    public CharInfo(float r, float h)
    {
        radius = r; height = h;
        halfradius = r / 2f; halfheight = h / 2f;
        rayDistance = halfheight + radius + .175f;
    }
}