using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//not disabled per player: exists all the time
public class Hurtbox : NetworkBehaviour
{
    public NetworkVariable<int> playerHP = new NetworkVariable<int>();
    private const int initialHP = 10;
    [SerializeField] private GameObject player;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Respawn();
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
            Respawn();
        }
    }
    private void Respawn()
    {
        playerHP.Value = initialHP;
        player.transform.position = Vector3.zero;//RespawnManager.Instance.respawnPoints[Random.Range(0, RespawnManager.Instance.respawnPoints.Count)].position;
    }
}
