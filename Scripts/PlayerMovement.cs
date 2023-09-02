using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float jumpHeight = 3f;
    public float defaultSpeed = 12f;
    public float sprintMult = 1.3f;
    public float groundRadius = 0.4f; //radius 
    public Rigidbody body;
    private float moveSpeed;
    private readonly float gravity = -19.62f;//-9.81f;

    public Transform groundCheck; //position to check ground at
    public LayerMask groundMask;

    private bool isGrounded;

    private Vector3 velocity;

    [SerializeField] private Animator animator;
    public enum MovementStates
    {
        Idle, Walking, Jumping, Wallrunning
    }
    public MovementStates playerMovementState = MovementStates.Idle;

    private void Update()
    { 
        CheckJumping();
    }
    void FixedUpdate()
    {
        Move(); 
    }
    private float x;
    private float z;
    private Vector3 moveDirection;
    [SerializeField] private TMP_Text speed;
    void Move()
    { 
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        moveDirection = transform.right * x + transform.forward * z;
        Vector3 horizontalMoveDir = transform.right * x;
        moveSpeed = defaultSpeed;
        /*if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = defaultSpeed * sprintMult;
        }
        else
        {
            moveSpeed = defaultSpeed;
        }*/

        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        if (isGrounded)
        { 
            body.AddForce(moveSpeed * moveDirection.normalized * 10, ForceMode.Force);
        }
        else if (playerMovementState == MovementStates.Wallrunning)
        { 
            if ((wallLeft && x<0) || (wallRight && x > 0))
            { 
                body.AddForce(-wallNormal * 100, ForceMode.Force);
            }
            else
            {
                body.AddForce(moveSpeed * horizontalMoveDir.normalized * 10, ForceMode.Force);
            }
        }
        /*else
        { 
            body.AddForce(moveSpeed * horizontalMoveDir.normalized * 1, ForceMode.Force);
        }*/

        WallRunMovement();
        UpdateDrag();
        UpdateState();
        UpdateAnimation();
        speed.text = "Speed: " + body.velocity.magnitude;
    }
    private void UpdateDrag()
    {
        if (isGrounded)
        {
            body.drag = groundDrag;
        }
        else if (playerMovementState == MovementStates.Wallrunning)
        { 
            body.drag = groundDrag;
        }
        else
        {
            body.drag = .1f;
        }
    }
    private float groundDrag = 8f; 
    private void CheckJumping()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (playerMovementState == MovementStates.Wallrunning)
            {
                WallRunJump();
            }
            else if (isGrounded)
            {
                Jump();
            } 
        }  
    }
    private void WallRunJump()
    { 
        body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
        body.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
        body.AddForce(wallNormal * jumpHeight * .75f, ForceMode.Impulse);
    }
    private void Jump()
    { 
        body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
        body.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
    }
    private void UpdateState()
    { 
        if (isGrounded)
        {
            if (body.velocity.sqrMagnitude < .1f)
            {
                playerMovementState = MovementStates.Idle;
            }
            else
            {
                playerMovementState = MovementStates.Walking;
            }
        }
        else
        {
            if (CheckWalls())
            {
                playerMovementState = MovementStates.Wallrunning;
            }
            else
            { 
                playerMovementState = MovementStates.Jumping;
            }
        }
    }
    private void UpdateAnimation()
    {
        switch (playerMovementState)
        {
            case MovementStates.Idle:
                animator.Play("Idle");
                break;
            case MovementStates.Wallrunning: 
            case MovementStates.Walking:
                animator.Play("Walk");
                break;
            case MovementStates.Jumping:
                animator.Play("Jump");
                break;
            default:
                break;
        }
    }
    private bool wallRight;
    private bool wallLeft;
    private RaycastHit outRightHit;
    private RaycastHit outLeftHit;
    private float wallCheckDist = .75f;
    [SerializeField] private LayerMask wallMask;
    private bool CheckWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out outRightHit, wallCheckDist, wallMask);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out outLeftHit, wallCheckDist, wallMask); 
        return wallLeft || wallRight;
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        Debug.DrawRay(transform.position, transform.right * wallCheckDist, Color.white);
    }
    private Vector3 wallNormal;
    private void WallRunMovement()
    {
        if ((playerMovementState == MovementStates.Wallrunning) && z > 0 & !isGrounded)
        {

            wallNormal = wallRight ? outRightHit.normal : outLeftHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((transform.forward - wallForward).magnitude > (transform.forward + wallForward).magnitude){
                wallForward = -wallForward;
            }
             
            body.AddForce(wallForward * moveSpeed * 15, ForceMode.Force);
            if (!(wallLeft && x > 0) && !(wallRight && x < 0)) //stick on
            {
                //body.AddForce(-wallNormal * 100, ForceMode.Force); 
                if (body.velocity.y <= 0)
                {
                    body.useGravity = false;
                    //body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
                    body.AddForce(transform.up * gravity / 2, ForceMode.Force);
                }
            } 
        }
        else
        {
            body.useGravity = true;
        }
    }
}
