using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        Idle, Walking, Jumping
    }
    public MovementStates playerMovementState = MovementStates.Idle;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
    private void Update()
    { 
        Jump();
    }
    void FixedUpdate()
    {
        Move();
    }
    private float x;
    private float z;
    private Vector3 moveDirection;
    void Move()
    {
        //RaycastHit hit;
        //isGrounded = Physics.SphereCast(groundCheck.position, groundRadius, Vector3.down, out hit, 0, groundMask);
        /*if (isGrounded)
        {
            Rigidbody body = hit.collider.GetComponentInParent<Rigidbody>();
            body.AddForceAtPosition(Vector3.down * 10, transform.position, ForceMode.Impulse);
        }*/
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask); 
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        moveDirection = transform.right * x + transform.forward * z;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = defaultSpeed * sprintMult;
        }
        else
        {
            moveSpeed = defaultSpeed;
        }
         
        if (isGrounded)
        {
            body.AddForce(moveSpeed * moveDirection.normalized * 10, ForceMode.Force); 
        }

        //WallRunMovement();
        UpdateDrag();
        UpdateState();
        UpdateAnimation();
    }
    private float groundDrag = 8f;
    private void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            //velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); //some physics equation for velocity needed to jump a height

            body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
            body.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
        } 
    }
    private void UpdateDrag()
    {
        if (isGrounded)
        {
            body.drag = groundDrag;
        }
        else
        {
            body.drag = 0;
        }
    }
    private void UpdateState()
    {

        if (isGrounded)
        {
            if (moveDirection.sqrMagnitude < .1f)
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
            playerMovementState = MovementStates.Jumping;
        }
    }
    private void UpdateAnimation()
    {
        switch (playerMovementState)
        {   
            case MovementStates.Idle:
                animator.Play("Idle");
                break;
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
    private float wallCheckDist = 0.6f;
    [SerializeField] private LayerMask wallMask;
    private void CheckWalls()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out outRightHit, wallCheckDist, wallMask);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out outLeftHit, wallCheckDist, wallMask);
    }
    private void WallRunMovement()
    {
        CheckWalls();
        if ((wallRight || wallLeft) && z > 0 & !isGrounded)
        {
            Vector3 wallNormal = wallRight ? outRightHit.normal : outLeftHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        } 
    }
}
