using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    CharacterMovement movement;
    public void Start()
    {
        movement = this.GetComponentInParent<CharacterMovement>();
    }


    public void OnTriggerEnter(Collider other)
    {
        Vector3 playerPoint = this.transform.position;
        Vector3 contactVector = other.ClosestPoint(playerPoint);
        Vector3 forward = this.transform.forward;

        Vector3 towardsOther = contactVector - playerPoint;
        float angle = Vector3.Angle(forward, towardsOther);     //collision angle 0° is direct collision
        float percentage = angle / 180;
        percentage = 1 - percentage;

        //Debug.Log(other.transform.name + " " + percentage);

        if (!other.gameObject.name.Equals("Player"))
        {
            movement.CollisionSlow(percentage);
        }
    }
}
