using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ArmyBase : NetworkBehaviour
{
    public int team = 0;
    [SerializeField] private AISoldier soldierPrefab;
    [SerializeField] private AISoldier artilleryPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    public List<AISoldier> spawnedSoldiers;
    private readonly int maxSoldiers = 50; //not including players...
    private readonly int artilleryPerWave = 10; 
    public override void OnNetworkSpawn()
    { 
        if (IsServer)
        { 
            InvokeRepeating(nameof(ReinforcementWave), 0, 60);
        }
    }
    public void CullDeadSoldiers()
    { 
        //clear empty spots

        for (int i = spawnedSoldiers.Count - 1; i >= 0; i--)
        {
            if (!spawnedSoldiers[i].hurtbox.alive)
            {
                spawnedSoldiers.RemoveAt(i);
            }
        }
    }
    private void ReinforcementWave()
    {
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
    }
    private void SpawnSoldierUmbrella(Transform trans, AISoldier prefab)
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(prefab, trans.position, Quaternion.identity);
            //soldier.netObj.SpawnWithOwnership(OwnerClientId);
            soldier.netObj.Spawn();
            soldier.hurtbox.team.Value = team;
            soldier.SetLayers();
            spawnedSoldiers.Add(soldier);

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
