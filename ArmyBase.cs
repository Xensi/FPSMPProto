using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ArmyBase : NetworkBehaviour
{
    public int team = 0;
    [SerializeField] private AISoldier soldierPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    public override void OnNetworkSpawn()
    { 
        if (IsServer)
        { 
            InvokeRepeating(nameof(ReinforcementWave), 0, 60);
        }
    }
    private void ReinforcementWave()
    {
        foreach (Transform item in spawnPoints)
        {
            SpawnSoldierUmbrella(item);
        }
    }
    private void SpawnSoldierUmbrella(Transform trans)
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(soldierPrefab, trans.position, Quaternion.identity);
            //soldier.netObj.SpawnWithOwnership(OwnerClientId);
            soldier.netObj.Spawn();
            soldier.hurtbox.team.Value = team;
            soldier.SetLayers();
        }
        /*else
        {
            //tell server to spawn
            SpawnSoldierServerRpc();
        }*/
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
