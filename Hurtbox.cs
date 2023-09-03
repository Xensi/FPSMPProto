using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//not disabled per player: exists all the time
public class Hurtbox : NetworkBehaviour
{
    public NetworkVariable<int> playerHP = new NetworkVariable<int>();
    private const int initialHP = 5;
    [SerializeField] private GameObject player;
    public override void OnNetworkSpawn()
    { 
        Respawn();
    }
    private void Respawn()
    {
        if (IsServer)
        {
            playerHP.Value = initialHP;
        }
        SpawnRandom();
    }
    private void SpawnRandom()
    {
        if (IsOwner)
        {
            player.transform.position = RespawnManager.Instance.respawnPoints[Random.Range(0, RespawnManager.Instance.respawnPoints.Count)].position;
        } 
    }
    private void Update()
    {
        if (IsServer)
        { 
            CheckIfDead();
        }
    }
    private void CheckIfDead()
    {
        if (playerHP.Value <= 0)
        { 
            playerHP.Value = initialHP;
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
            RespawnClientRpc();
        }
    }
    [ClientRpc]
    private void RespawnClientRpc(ClientRpcParams clientParams = default)
    {
        SpawnRandom();
    }
}
