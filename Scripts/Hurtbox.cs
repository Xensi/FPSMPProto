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
        if (bloodEffect != null)
        {
            bloodEffect.Stop();
            bloodEffect.gameObject.SetActive(false);
        }
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
            /*if (playerControlled)
            { 
                PlayerCheckIfDead();
            }
            else
            {
                AICheckIfDead();
            }*/
            UpdateBleedOut();
        }
    }
    private void UpdateBleedOut()
    {
        //when hit by a bullet or explosion, start bleeding out
        //being hit multiple times reduces bleed out time
        if (bleedOutTimer > 0) //bleeding
        {
            bleedOutTimer -= Time.deltaTime;
            if (bloodEffect != null)
            { 
                bloodEffect.gameObject.SetActive(true);
                bloodEffect.Play();
            }
        }
        else if (bleedOutTimer <= 0 && bleedOutTimer > -999)
        {
            bleedOutTimer = -999;
            if (bloodEffect != null)
            { 
                bloodEffect.Stop();
                bloodEffect.gameObject.SetActive(false);
            }
            FinishBleedingOut();
        }
        else
        { 
            if (bloodEffect != null)
            {
                bloodEffect.Stop();
                bloodEffect.gameObject.SetActive(false);
            }
        }
    }
    private void FinishBleedingOut()
    { 
        if (playerControlled)
        { 
            SpawnRandom();
        }
        else
        {
            AIDeath();
        }
    }
    private void GetHitBleedOut(float reduceBleedOutOnHit)
    {
        if (bleedOutTimer == -999)
        {
            bleedOutTimer = maxBleedOutTime - reduceBleedOutOnHit;
            Debug.Log(bleedOutTimer);
        }
        else
        {
            bleedOutTimer -= reduceBleedOutOnHit;
        }
    }
    private void AIDeath()
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
            Vector3 random = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            soldier.body.AddForceAtPosition(random.normalized, soldier.transform.position + new Vector3(0, 2, 0), ForceMode.Impulse);
            Invoke(nameof(DestroyThis), 60);
        }
    }
    public bool alive = true;

    private float maxBleedOutTime = 10;
    private float reduceBleedOutOnHit = 1; 
    private float bleedOutTimer = -999;
    private void AICheckIfDead()
    {
        if (HP.Value <= 0 && alive)
        {
            AIDeath();
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
    [SerializeField] private ParticleSystem bloodEffect;

    public void DealDamageUmbrella(int damage)
    {
        float adjusted = damage / maxBleedOutTime;
        GetHitBleedOut(adjusted);
       /* if (IsServer) //server can write network variables
        {
            DealDamage(damage);
        }
        else //ask server to write network variable
        {
            DealDamageServerRpc(damage);
        }*/
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
