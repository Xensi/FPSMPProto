using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DisableBehavioursForNonOwners : NetworkBehaviour
{   //disable components that are used only by the client on other players' games
    public List<Behaviour> behaviours;
    public List<Renderer> renderers;
    //cosmetic items should remain enabled, like jolt
    public override void OnNetworkSpawn()
    {  
        if (!IsOwner) //disable client-only functionality
        {
            foreach (Behaviour item in behaviours)
            {
                item.enabled = false;
            }

            foreach (Renderer item in renderers)
            {
                item.enabled = false;
            }
            enabled = false; 
        } 

        if (!IsServer) //if we are not the server, then tell global to update terrain for us
        {
            RequestPreviousTerrainChangesServerRpc(OwnerClientId);
        } 
    } 
    [ServerRpc (RequireOwnership = false)]
    private void RequestPreviousTerrainChangesServerRpc(ulong clientId)
    { 
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        foreach (Global.DigEvent item in Global.Instance.digEvents)
        {   
            SendPreviousTerrainChangesClientRpc(item.position, item.type, clientRpcParams);
        }
    }  
    [ClientRpc]
    private void SendPreviousTerrainChangesClientRpc(Vector3 position, Global.DigType type, ClientRpcParams clientRpcParams = default)
    { //float strength, int width, int height, 
        Global.DigEvent dig = new();
        dig.position = position;
        dig.type = type;
        /*dig.strength = strength;
        dig.brushWidth = width;
        dig.brushHeight = height;*/ 
        Global.Instance.digEvents.Add(dig);
        Global.Instance.Dig(dig); 
    }
}
