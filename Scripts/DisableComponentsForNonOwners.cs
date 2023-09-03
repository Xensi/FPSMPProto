using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DisableComponentsForNonOwners : NetworkBehaviour
{   //disable components that are used only by the client on other players' games
    public List<Behaviour> components; 
    //cosmetic items should remain enabled, like jolt
    public override void OnNetworkSpawn()
    {  
        if (!IsOwner) //disable client-only functionality
        {
            foreach (Behaviour item in components)
            {
                item.enabled = false;
            }
             
            enabled = false; 
        } 
    }
}
