using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    CharacterMovement movement;
    public bool debugDraw = false;
    Vector3 lastPos = Vector3.zero;
    public Vector3 trajectory = Vector3.zero;


    public bool active = true;
    public void Start()
    {
        movement = this.GetComponentInParent<CharacterMovement>();
    }

    public void FixedUpdate()
    {
        trajectory = transform.position - lastPos;
        trajectory = trajectory.normalized;
        trajectory += transform.position;
        lastPos = transform.position;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.GetType().Name.Equals("MeshCollider"))        //closest point on a mesh collider doesnt work
            return;

        Vector3 playerPoint = this.transform.position;
        Vector3 contactVector = other.ClosestPoint(playerPoint);
        Vector3 forward = this.transform.forward;

        Vector3 towardsOther = contactVector - playerPoint;
        float angle = Vector3.Angle(forward, towardsOther);     //collision angle 0° is direct collision
        float percentage = angle / 180;
        percentage = 1 - percentage;

        if (!other.gameObject.name.Equals("Player"))
            if(active)
            { 
                movement.CollisionSlow(percentage);
            }
    }

    public bool TrajectoryBlocked()
    {
        return Physics.CheckSphere(trajectory, 0.2f) || 
               Physics.CheckSphere(new Vector3(trajectory.x, trajectory.y + 0.5f, trajectory.z), 0.2f) || 
               Physics.CheckSphere(new Vector3(trajectory.x, trajectory.y - 0.5f, trajectory.z), 0.2f);
    }

    //used for testing
    private void OnDrawGizmos()
    {
        if(debugDraw)
        { 
            Gizmos.DrawSphere(trajectory, 0.2f);
            Gizmos.DrawSphere(new Vector3(trajectory.x, trajectory.y + 0.5f, trajectory.z), 0.2f);
            Gizmos.DrawSphere(new Vector3(trajectory.x, trajectory.y - 0.5f, trajectory.z), 0.2f);
        }
    }

}
