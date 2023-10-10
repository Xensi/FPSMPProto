using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ArmyBase : NetworkBehaviour
{
    public int team = 0;
    [SerializeField] private List<AISoldier> soldierPrefabs;
    [SerializeField] private AISoldier soldierPrefab;
    [SerializeField] private AISoldier artilleryPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    public List<AISoldier> spawnedSoldiers;
    //private readonly int maxSoldiers = 50; //not including players...
    private readonly int maxSoldiers = 50; //not including players...
    private readonly int artilleryPerWave = 10;
    public bool spawnSoldiers = false;
    public List<Transform> playerSpawns;
    public override void OnNetworkSpawn()
    { 
        if (IsServer)
        { 
            //InvokeRepeating(nameof(ReinforcementWave), 0, 60);
        }
    }
    /*private void ReinforcementWave()
    {
        if (!spawnSoldiers) return;
        CullDeadSoldiers();
        int x = 0;
        foreach (Transform item in spawnPoints)
        {
            if (spawnedSoldiers.Count < maxSoldiers)
            { 
                if (x < artilleryPerWave)
                { 
                    SpawnSoldierUmbrella(item, artilleryPrefab);
                }
                else
                {
                    SpawnSoldierUmbrella(item, soldierPrefab);
                }
                x++;
            }
            else
            {
                break;
            }
        }
    }*/
    public void RecruitSoldier(int type = 0, ulong id = 0)
    {
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Count)];
        AISoldier prefab = soldierPrefabs[type];
        SpawnSoldierUmbrella(spawn, prefab, id);
    }
    private void SpawnSoldierUmbrella(Transform trans, AISoldier prefab, ulong id)
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(prefab, trans.position, Quaternion.identity);
            //soldier.netObj.SpawnWithOwnership(id);
            soldier.netObj.Spawn();
            soldier.hurtbox.team.Value = team;
            soldier.SetLayers();
            //spawnedSoldiers.Add(soldier);
            //send message to client adding soldier to their list 
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { id }
                }
            };
            AddSoldierClientRpc(soldier, clientRpcParams);
        } 
    } 
    [ClientRpc]
    private void AddSoldierClientRpc(NetworkBehaviourReference soldier, ClientRpcParams clientRpcParams = default)
    {
        if (soldier.TryGet(out AISoldier sol))
        { 
            SpawnSoldier spawner = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<SpawnSoldier>();
            spawner.commandableSoldiers.Add(sol);
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
