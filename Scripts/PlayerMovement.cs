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
    public CharacterController controller;
    private float moveSpeed;
    private float gravity = -19.62f;//-9.81f;

    public Transform groundCheck; //position to check ground at
    public LayerMask groundMask;

    private bool isGrounded;

    private Vector3 velocity; 
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
    void Update()
    {
        Move();
    }
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
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; //negative to force onto ground
        }
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed = defaultSpeed * sprintMult;
        }
        else
        {
            moveSpeed = defaultSpeed;
        }

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); //some physics equation for velocity needed to jump a height
        }

        //gravity
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
}
