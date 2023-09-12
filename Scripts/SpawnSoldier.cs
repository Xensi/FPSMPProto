using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using Unity.Netcode;
public class SpawnSoldier : NetworkBehaviour
{
    [SerializeField] private AISoldier soldierPrefab;
    public List<AISoldier> ownedSoldiers;

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            SpawnSoldierUmbrella();
        }
        if (Input.GetMouseButtonDown(1))
        {
            CommandSoldiers();
        }
    }
    private void CommandSoldiers()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
            item.target = transform.position;
        }
    }
    private void SpawnSoldierUmbrella() 
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
            soldier.netObj.SpawnWithOwnership(OwnerClientId); 
        }
        else
        {
            //tell server to spawn
            SpawnSoldierServerRpc();
        }
    }
    /// <summary>
    /// Non-server client tells server to spawn in a minion and grant ownership to the client.
    /// </summary> 
    [ServerRpc]
    private void SpawnSoldierServerRpc(ServerRpcParams serverRpcParams = default)
    {
        AISoldier soldier = Instantiate(soldierPrefab, transform.position, Quaternion.identity);

        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            soldier.netObj.SpawnWithOwnership(clientId); 
        }
        //server puts new soldier in client's list
    }  
}
