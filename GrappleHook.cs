using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float grappleDistance = Mathf.Infinity;
    [SerializeField] private LayerMask grappleMask;
    private SpringJoint joint;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (joint == null)
            { 
                Grapple();
            } 
        }
        if (Input.GetKey(KeyCode.Q))
        {
            if (joint != null)
            {
                ShortenCable();
            }
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            if (joint != null)
            {
                StopGrapple();
            }
        }
    }
    [SerializeField] private float drawInSpeed = 1000;
    private void ShortenCable()
    {
        Vector3 dir = grapplePoint.position - grappleOrigin.position;
        body.AddForce(dir.normalized * drawInSpeed * Time.deltaTime, ForceMode.Force);
        float distFromPoint = Vector3.Distance(player.transform.position, grapplePoint.position);

        joint.maxDistance = distFromPoint * 0.8f;
        joint.minDistance = distFromPoint * .2f;
    }

    private void StopGrapple()
    {
        line.positionCount = 0;
        Destroy(joint);
    }
    private void LateUpdate()
    {
        if (joint != null)
        { 
            DrawRope();
        }
    }
    [SerializeField] private Rigidbody body;
    [SerializeField] private LineRenderer line;
    private void DrawRope()
    {
        line.SetPosition(0, grappleHand.position);
        line.SetPosition(1, grapplePoint.position);
    }
    [SerializeField] private Transform grapplePoint;
    [SerializeField] private Transform grappleOrigin;
    [SerializeField] private Transform grappleHand;
    private void Grapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(grappleOrigin.position, grappleOrigin.forward, out hit, grappleDistance, grappleMask)) //if raycast hits something  
        {
            grapplePoint.position = hit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = hit.point;

            float distFromPoint = Vector3.Distance(player.transform.position, hit.point);

            joint.maxDistance = distFromPoint * 0.8f;
            joint.minDistance = distFromPoint * .2f;

            joint.spring = 4.5f;
            joint.damper = 7;
            joint.massScale = 4.5f;

            line.positionCount = 2;
        }
    }
}
