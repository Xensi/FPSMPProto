using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DisableComponentsForNonOwners : NetworkBehaviour
{   //disable components that are used only by the client on other players' games
    public BasicPlayerMovement movement;
    public MouseLook look;
    public AudioListener listener;
    public Camera cam;
    public Camera gunCam;
    public BasicShoot shoot; 
    //cosmetic items should remain enabled, like jolt
    public override void OnNetworkSpawn()
    {  
        if (!IsOwner) //disable client-only functionality
        {
            if (movement != null) movement.enabled = false;
            if (listener != null) listener.enabled = false;
            if (cam != null) cam.enabled = false;
            if (gunCam != null) gunCam.enabled = false;
            if (look != null) look.enabled = false;
            if (shoot != null) shoot.enabled = false;
             
            enabled = false; 
        } 
    }
}
