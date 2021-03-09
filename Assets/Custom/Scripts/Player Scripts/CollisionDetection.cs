using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    CharacterMovement movement;
    Vector3 contactPoint;
    public void Start()
    {
        movement = this.GetComponentInParent<CharacterMovement>();
    }

    public void OnTriggerEnter(Collider other)
    {
        contactPoint = other.ClosestPoint(this.transform.parent.transform.position);
        //calculate a percentage slow based on the players angle on collision
        //if they're facing 0° from it 100% slow
        //if they're facing 90° from it 0% slow
        //linear in between
        //penalty for running into objects
        if (!other.gameObject.name.Equals("Player"))
        {
            movement.CollisionSlow();
        }
    }
}
