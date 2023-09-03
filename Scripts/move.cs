using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class move : MonoBehaviour
{ 
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

    void Start()
    { 
        Cursor.lockState = CursorLockMode.Locked;
        moveSpeed = defaultSpeed;
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
        orientation.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        manualRot = Quaternion.Euler(xRotation, yRotation, 0);
    }
    private Quaternion manualRot;
    private void Update()
    {
        LookAround();
        CheckJumping(); 
    }   
    void FixedUpdate()
    {
        Movement(); 
    }
    [SerializeField] private Transform orientation;
    private void Walk()
    {
        //try tying to camera angle?
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.E))
        {
            moveDirection = orientation.forward * z;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            moveDirection = manualRot * Vector3.forward * z;
        }
        else
        { 
            moveDirection = transform.forward * z;
        }
        //Vector3 horizontalMoveDir = transform.right * x;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        if (isGrounded && playerMovementState != MovementStates.Sliding)
        {
            body.AddForce(10 * moveSpeed * moveDirection.normalized, ForceMode.Force);
        }
        /*else if (playerMovementState == MovementStates.Wallrunning)
        {
            if ((wallLeft && x < 0) || (wallRight && x > 0))
            {
                body.AddForce(-wallNormal * 100, ForceMode.Force);
            }
            else
            {
                body.AddForce(10 * moveSpeed * horizontalMoveDir.normalized, ForceMode.Force);
            }
        }*/
    }
    void Movement()
    {
        Walk();
        //WallRunMovement();
        UpdateDrag();
        //UpdateState(); 
        //speed.text = "Speed: " + body.velocity.magnitude;
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
        body.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
        body.AddForce(.75f * jumpHeight * wallNormal, ForceMode.Impulse);
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
    private void WallRunMovement()
    {
        if ((playerMovementState == MovementStates.Wallrunning) && z > 0 & !isGrounded)
        {

            wallNormal = wallRight ? outRightHit.normal : outLeftHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((transform.forward - wallForward).magnitude > (transform.forward + wallForward).magnitude){
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
                    body.AddForce(transform.up * -9.81f / 2, ForceMode.Force);
                }
            } 
        }
        else
        {
            body.useGravity = true;
        }
    }
}
