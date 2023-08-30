using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    public PlayerMovement movement;
    public MouseLook look;
    public AudioListener listener;
    public Camera cam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            movement.enabled = false;
            listener.enabled = false;
            cam.enabled = false;
            look.enabled = false;
            enabled = false;
            
        } 
    }
}
