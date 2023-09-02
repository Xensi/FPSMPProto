using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 400f;

    public Transform playerBody;
    public Transform camPos;

    private float xRotation;
    private float yRotation;

    void Start()
    {
        /*Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;*/
        Cursor.lockState = CursorLockMode.Locked;
    }
     
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90); //prevent over rotation

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0); //rotate camera locally 
        transform.position = camPos.position;
        playerBody.SetPositionAndRotation(playerBody.transform.position, Quaternion.Euler(0, yRotation, 0));
         
    }
}
