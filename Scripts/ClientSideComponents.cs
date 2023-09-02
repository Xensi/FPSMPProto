using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ClientSideComponents : NetworkBehaviour
{   //disable components that are used only by the client on other players' games
    public PlayerMovement movement;
    public MouseLook look;
    public AudioListener listener;
    public Camera cam;
    public BasicShoot shoot; 
    //cosmetic items should remain enabled, like jolt
    public override void OnNetworkSpawn()
    {  
        if (!IsOwner) //disable client-only functionality
        {
            movement.enabled = false;
            listener.enabled = false;
            cam.enabled = false;
            look.enabled = false;
            shoot.enabled = false;
             
            enabled = false; 
        } 
    }
}
