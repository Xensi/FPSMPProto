using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//not disabled per player: exists all the time
public class Hurtbox : NetworkBehaviour
{
    public NetworkVariable<int> playerHP = new NetworkVariable<int>();
    private const int initialHP = 100;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerHP.Value = initialHP;
        }
    }
}
