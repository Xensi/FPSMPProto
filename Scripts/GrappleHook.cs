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
    public SpringJoint joint;
    private bool retractionInProgress = false;
    private bool grappleInProgress = false;
    private bool grappleLanded = false;
    private bool grappleReady = true;
    private Vector3 grappleHitPoint;
    [SerializeField] private float drawInSpeed = 5000;
    [SerializeField] private float shootOutSpeed = 10;
    public NetworkVariable<bool> drawGrapple = new NetworkVariable<bool>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector3> grappleTip = new NetworkVariable<Vector3>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private Rigidbody body;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Vector3 drawGrapplePoint;
    [SerializeField] private Transform grappleOrigin;
    [SerializeField] private Transform grappleHand;
    public enum GrappleStates
    {
        ReadyToFire, ShootingOut, PullingPlayer, Retracting
    }
    public GrappleStates grappleState = GrappleStates.ReadyToFire;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        { 
            drawGrapple.Value = false;
        }
    }
    void Update()
    {
        if (IsOwner)
        {
            HandleStates(); 
        }
        else
        {
            if (drawGrapple.Value == true)
            {
                DrawRopeNonOwner();
            }
            else
            {
                line.positionCount = 0;
            }
        }
    }
    private void DrawRope()
    {
        if (line.positionCount == 2)
        {
            line.SetPosition(0, grappleHand.position);
            line.SetPosition(1, drawGrapplePoint);
        }
    }
    private void DrawRopeNonOwner()
    {
        line.positionCount = 2;
        line.SetPosition(0, grappleHand.position);
        line.SetPosition(1, grappleTip.Value);
    }  
    private void HandleStates()
    {
        switch (grappleState)
        {
            case GrappleStates.ReadyToFire: 
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(grappleOrigin.position, grappleOrigin.forward, out hit, grappleDistance, grappleMask)) //if raycast hits something  
                    {
                        grappleState = GrappleStates.ShootingOut;
                        grappleHitPoint = hit.point;
                        drawGrapplePoint = grappleHand.position;
                        line.positionCount = 2;
                        drawGrapple.Value = true;
                    }
                }
                break;
            case GrappleStates.ShootingOut: 
                if (Input.GetKeyUp(KeyCode.Q))
                {
                    grappleState = GrappleStates.Retracting;
                    if (joint != null)
                    { 
                        Destroy(joint);
                    }
                }
                else
                {
                    if (Vector3.Distance(drawGrapplePoint, grappleHitPoint) > .1f)
                    {
                        drawGrapplePoint = Vector3.MoveTowards(drawGrapplePoint, grappleHitPoint, shootOutSpeed * Time.deltaTime);
                        grappleTip.Value = drawGrapplePoint;
                    }
                    else //start reeling in player
                    {
                        grappleState = GrappleStates.PullingPlayer;
                    } 
                }
                DrawRope();
                break;
            case GrappleStates.PullingPlayer:
                if (Input.GetKeyUp(KeyCode.Q))
                {
                    grappleState = GrappleStates.Retracting;
                    if (joint != null)
                    {
                        Destroy(joint);
                    }
                }
                else
                {
                    if (joint == null)
                    {
                        CreateJoint();
                    }
                    ShortenCable();
                } 
                DrawRope();
                break;
            case GrappleStates.Retracting:
                if (Vector3.Distance(drawGrapplePoint, grappleHand.position) > 1f)
                {
                    drawGrapplePoint = Vector3.MoveTowards(drawGrapplePoint, grappleHand.position, shootOutSpeed * Time.deltaTime);
                    grappleTip.Value = drawGrapplePoint;
                }
                else
                {
                    grappleState = GrappleStates.ReadyToFire;
                    line.positionCount = 0;
                    drawGrapple.Value = false;
                }
                DrawRope();
                break;
            default:
                break;
        }
    } 
    private void CreateJoint()
    { 
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grappleHitPoint;

        float distFromPoint = Vector3.Distance(player.transform.position, grappleHitPoint);

        joint.maxDistance = distFromPoint * 0.8f;
        joint.minDistance = distFromPoint * .2f;

        joint.spring = 4.5f;
        joint.damper = 7;
        joint.massScale = 4.5f;

        line.positionCount = 2;
    }
    private void ShortenCable()
    {
        Vector3 dir = drawGrapplePoint - grappleOrigin.position;
        body.AddForce(dir.normalized * drawInSpeed * Time.deltaTime, ForceMode.Force);
        float distFromPoint = Vector3.Distance(player.transform.position, drawGrapplePoint);

        joint.maxDistance = distFromPoint * 0.8f;
        joint.minDistance = distFromPoint * .2f;
    }  
}
