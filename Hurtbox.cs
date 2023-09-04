using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//not disabled per player: exists all the time
public class Hurtbox : NetworkBehaviour
{
    public NetworkVariable<int> playerHP = new NetworkVariable<int>();
    private const int initialHP = 100;
    [SerializeField] private GameObject player;
    [SerializeField] private List<Transform> playerObjectsToChangeLayers; //assign visuals
    [SerializeField] private Rigidbody body; 
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
    void SetLayerAllChildren(Transform root, int layer)
    {
        var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (var child in children)
        {
            Debug.Log(child.name);
            child.gameObject.layer = layer;
        }
    }
    private void SpawnRandom()
    {
        if (IsOwner)
        {
            player.transform.position = RespawnManager.Instance.respawnPoints[Random.Range(0, RespawnManager.Instance.respawnPoints.Count)].position; 
        }
        else
        {
            player.layer = 7;
            foreach (Transform item in playerObjectsToChangeLayers)
            {
                SetLayerAllChildren(item, 7);
            }
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

    public void DealDamageUmbrella(int damage)
    {
        if (IsServer) //server can write network variables
        {
            DealDamage(damage);
        }
        else //ask server to write network variable
        {
            DealDamageServerRpc(damage);
        }
    }
    private void DealDamage(int damage)
    {
        playerHP.Value -= damage;
    }
    [ServerRpc (RequireOwnership = false)]
    private void DealDamageServerRpc(int damage)
    {
        DealDamage(damage);
    }
}
