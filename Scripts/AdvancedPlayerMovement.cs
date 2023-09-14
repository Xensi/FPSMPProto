using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
public class AdvancedPlayerMovement : NetworkBehaviour
{
    [SerializeField] protected Animator animator;
    [SerializeField] protected TMP_Text speed;
    public Rigidbody body;
    [SerializeField] protected float jumpDistance = 40f;
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
    private bool wallRight;
    private bool wallLeft;
    private RaycastHit outRightHit;
    private RaycastHit outLeftHit;
    private readonly float wallCheckDist = .75f;
    [SerializeField] private LayerMask wallMask;
    private Vector3 wallNormal;

    private float mouseSensitivity = 400f;  

    private float xRotation;
    private float yRotation;
    [SerializeField] private Transform keepUpright; 
    [SerializeField] private bool canWallRun = true;

    void Start()
    { 
        Cursor.lockState = CursorLockMode.Locked;
        moveSpeed = defaultSpeed;
    }
    private void Update()
    {
        LookAround();
        CheckJumping();
    }
    private void LateUpdate()
    {
        AngleAdjust(); 
    }
    void FixedUpdate()
    {
        Movement();
    }
    private void LookAround()
    {
        // up and down synced, side to side not synced ???
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90); //prevent over rotation

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0); //rotate camera locally   
    }
    [SerializeField] private Transform camJoltRef;
    private void AngleAdjust()
    {
        //keepUpright.localRotation = Quaternion.Euler(-xRotation, 0, 0);
        keepUpright.localRotation = Quaternion.Euler(-camJoltRef.rotation.eulerAngles.x, 0, 0); 
        //keepUpright.rotation = 
    } 
    void Movement()
    {
        Walk();
        if (canWallRun) WallRunMovement();
        UpdateDrag();
        UpdateState();
        UpdateAnimation();
        //speed.text = "Speed: " + body.velocity.magnitude;
    }
    private void Walk()
    {
        //try tying to camera angle?
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        moveDirection = keepUpright.right * x + keepUpright.forward * z;
        Vector3 horizontalMoveDir = keepUpright.right * x;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        if (isGrounded && playerMovementState != MovementStates.Sliding)
        {
            body.AddForce(10 * moveSpeed * moveDirection.normalized, ForceMode.Force);
        }
        /*else if (isGrounded && playerMovementState == MovementStates.Sliding)
        {
            body.AddForce(10 * slideDownwardForce * -keepUpright.up, ForceMode.Force);
        }*/
        else if (playerMovementState == MovementStates.Wallrunning)
        {
            if ((wallLeft && x < 0) || (wallRight && x > 0))
            {
                body.AddForce(-wallNormal * 100, ForceMode.Force);
            }
            else
            {
                body.AddForce(10 * moveSpeed * horizontalMoveDir.normalized, ForceMode.Force);
            }
        }
    }
    private void WallRunMovement()
    {
        if ((playerMovementState == MovementStates.Wallrunning) && z > 0 & !isGrounded)
        {

            wallNormal = wallRight ? outRightHit.normal : outLeftHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, keepUpright.up);

            if ((keepUpright.forward - wallForward).magnitude > (keepUpright.forward + wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            body.AddForce(10 * moveSpeed * wallForward, ForceMode.Force);
            if (!(wallLeft && x > 0) && !(wallRight && x < 0)) //stick on
            {
                //body.AddForce(-wallNormal * 100, ForceMode.Force); 
                if (body.velocity.y <= 0)
                {
                    body.useGravity = false;
                    //body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
                    body.AddForce(0.75f * -9.81f * keepUpright.up, ForceMode.Force);
                }
            }
        }
        else
        {
            body.useGravity = true;
        }
    } 
    private void UpdateDrag()
    {
        if (isGrounded)
        {
            if (playerMovementState == MovementStates.Sliding)
            {
                body.drag = .1f;
            }
            else
            {
                body.drag = groundDrag;
            }
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
        body.AddForce(keepUpright.up * jumpDistance, ForceMode.Impulse);
        body.AddForce(.75f * jumpDistance * wallNormal, ForceMode.Impulse);
    }
    private void Jump()
    { 
        body.velocity = new Vector3(body.velocity.x, 0, body.velocity.z);
        body.AddForce(keepUpright.up * jumpDistance, ForceMode.Impulse);
    }
    private void UpdateState()
    { 
        if (isGrounded)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                playerMovementState = MovementStates.Sliding;
            }
            else
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
}
