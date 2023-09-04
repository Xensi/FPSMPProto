using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GrappleHook : NetworkBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float grappleDistance = Mathf.Infinity;
    [SerializeField] private LayerMask grappleMask;
    private SpringJoint joint;
    void Update()
    {
        if (IsOwner)
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
    }
    
    private void LateUpdate()
    {
        if (IsOwner)
        { 
            if (joint != null)
            {
                DrawRope();
            }
        }
        else
        {
            if (serverDisplayGrapple) DrawRope();
        }
    }
    [SerializeField] private float drawInSpeed = 1000;
    private void ShortenCable()
    {
        Vector3 dir = grapplePoint - grappleOrigin.position;
        body.AddForce(dir.normalized * drawInSpeed * Time.deltaTime, ForceMode.Force);
        float distFromPoint = Vector3.Distance(player.transform.position, grapplePoint);

        joint.maxDistance = distFromPoint * 0.8f;
        joint.minDistance = distFromPoint * .2f;
    }

    private void StopGrapple()
    {
        line.positionCount = 0;
        DisplayGrappleUmbrella(false, Vector3.zero);
        Destroy(joint);
    }
    [SerializeField] private Rigidbody body;
    [SerializeField] private LineRenderer line;
    private void DrawRope()
    {
        line.SetPosition(0, grappleHand.position);
        line.SetPosition(1, grapplePoint);
    }
    [SerializeField] private Vector3 grapplePoint;
    [SerializeField] private Transform grappleOrigin;
    [SerializeField] private Transform grappleHand;
    private void Grapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(grappleOrigin.position, grappleOrigin.forward, out hit, grappleDistance, grappleMask)) //if raycast hits something  
        {
            grapplePoint = hit.point;
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

            DisplayGrappleUmbrella(true, hit.point);
        }
    }
    private bool serverDisplayGrapple = false;
    private void DisplayGrappleUmbrella(bool val, Vector3 point)
    {
        if (IsServer)
        {
            DisplayGrappleClientRpc(val, point);
        }
        else
        {
            DisplayGrappleServerRpc(val, point);
        }
    }
    private void BaseDisplayGrapple(bool val, Vector3 point)
    {
        grapplePoint = point;
        serverDisplayGrapple = val;
        if (val)
        {
            line.positionCount = 2;
        }
        else
        {
            line.positionCount = 0;
        }
    }
    [ClientRpc]
    private void DisplayGrappleClientRpc(bool val, Vector3 point)
    {
        BaseDisplayGrapple(val, point);
    }
    [ServerRpc]
    private void DisplayGrappleServerRpc(bool val, Vector3 point)
    {
        DisplayGrappleClientRpc(val, point);
    }



}
