using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DisableBehavioursForNonOwners : NetworkBehaviour
{   //disable components that are used only by the client on other players' games
    public List<Behaviour> behaviours; 
    //cosmetic items should remain enabled, like jolt
    public override void OnNetworkSpawn()
    {  
        if (!IsOwner) //disable client-only functionality
        {
            foreach (Behaviour item in behaviours)
            {
                item.enabled = false;
            }
             
            enabled = false; 
        } 
    }
}
