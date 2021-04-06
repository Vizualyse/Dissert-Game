using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    public int grappleSide;
    public GrappleMove grappleScript;
    public LineRenderer rend;
    public Transform grapple;

    float time = 0f;
    LayerMask mask;
    Rigidbody rb;
    bool attached = false;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        mask = ~LayerMask.GetMask("Player");
    }

    private void Update()
    {
        if (attached)
            return;
        rend.enabled = true;
        if (Vector3.Distance(transform.position, grapple.position) > 100)
        {
            rend.enabled = false;
            Destroy(this.gameObject);
        }
        if (time > 2.5)
        {
            rend.enabled = false;
            Destroy(this.gameObject);
        }
        time += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (attached)
            this.enabled = false;

        if(Physics.CheckSphere(transform.position, 1f, mask))
        {
            grappleScript.attachGrapple(grappleSide, transform.position);
            //Destroy(this.gameObject);
            attached = true;
            rb.isKinematic = true;
        }
    }

    private void LateUpdate()
    {
        try { rend.SetPositions(new Vector3[] { grapple.position, transform.position }); } catch { }
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name == "Player")
            return;
        Vector3 cp = collision.GetContact(0).point;
        grappleScript.attachGrapple(grappleSide, cp);
        Destroy(this.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.name == "Player")
            return;
        Vector3 cp = collision.GetContact(0).point;
        grappleScript.attachGrapple(grappleSide, cp);
        Destroy(this.gameObject);
    }*/
}
