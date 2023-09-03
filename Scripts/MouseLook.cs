using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MouseLook : NetworkBehaviour
{
    private float mouseSensitivity = 400f;

    public Transform playerBody;
    //public Transform camPos;

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
        LookAround();
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
        //playerBody.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        //transform.position = camPos.position;
        //playerBody.SetPositionAndRotation(playerBody.transform.position, Quaternion.Euler(0, yRotation, 0));
        //playerBody.rotation = transform.rotation;

        /*if (!Input.GetKey(KeyCode.E))
        {
            
        }*/
    }
}
