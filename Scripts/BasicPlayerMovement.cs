using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class BasicPlayerMovement : NetworkBehaviour
{
    [SerializeField] protected Animator animator;
    [SerializeField] protected TMP_Text speed;
    public Rigidbody body;
    protected float jumpHeight = 40f;
    protected float defaultSpeed = 36f;
    protected float sprintMult = 1.3f;
    protected float groundRadius = 0.45f; //radius 
    protected bool isGrounded;
    protected float moveSpeed; 

    public Transform groundCheck; //position to check ground at
    public LayerMask groundMask;
    public enum MovementStates
    {
        Idle, Walking, Jumping, Wallrunning, Sliding
    }
    public MovementStates playerMovementState = MovementStates.Idle;

    protected float x;
    protected float z;
    protected Vector3 moveDirection;
    protected float groundDrag = 8f;  
    private void Update()
    {
        CheckJumping();
    }
    void FixedUpdate()
    {
        Movement();
    }
    private void Walk()
    {
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        moveDirection = transform.right * x + transform.forward * z; 
        moveSpeed = defaultSpeed;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        if (isGrounded)
        {
            body.AddForce(10 * moveSpeed * moveDirection.normalized, ForceMode.Force);
        } 
    }
    void Movement()
    {
        Walk(); 
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
        else
        {
            body.drag = .1f;
        }
    }
    private void CheckJumping()
    {
        if (Input.GetButtonDown("Jump"))
        { 
            if (isGrounded)
            {
                Jump();
            }
        }
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
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius); 
    } 
}
