using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//not disabled per player: exists all the time
public class Hurtbox : NetworkBehaviour
{ 
    public bool playerControlled = true;
    public NetworkVariable<int> HP = new();
    public NetworkVariable<int> team = new();
    private const int initialHP = 100;
    [SerializeField] private GameObject player;
    [SerializeField] private List<Transform> playerObjectsToChangeLayers; //assign visuals 
    public AISoldier soldier;
    public override void OnNetworkSpawn()
    { 
        Respawn();
    }
    private void Respawn()
    {
        if (IsServer)
        {
            HP.Value = initialHP;
        }
        if (playerControlled)
        { 
            SpawnRandom();
        }
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
            if (playerControlled)
            { 
                PlayerCheckIfDead();
            }
            else
            {
                AICheckIfDead();
            }
        }
    }
    public bool alive = true;
    private void AICheckIfDead()
    {
        if (HP.Value <= 0 && alive)
        {
            alive = false;
            if (soldier != null)
            {
                soldier.body.useGravity = true;
                soldier.body.isKinematic = false;
                soldier.body.drag = 0.1f;
                soldier.body.angularDrag = 1f;
                soldier.body.constraints = RigidbodyConstraints.None;
                soldier.pathfinder.enabled = false;
                soldier.enabled = false;
                Invoke(nameof(DestroyThis), 60);
            }
        }
    }
    private void DestroyThis()
    {
        Destroy(player.gameObject);
    }
    private void PlayerCheckIfDead()
    {
        if (HP.Value <= 0)
        { 
            HP.Value = initialHP;
            
            ClientRpcParams clientRpcParams = new()
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
        HP.Value -= damage;
    }
    [ServerRpc (RequireOwnership = false)]
    private void DealDamageServerRpc(int damage)
    {
        DealDamage(damage);
    }
}
