using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using Unity.Netcode;
public class SpawnSoldier : NetworkBehaviour
{
    [SerializeField] private AISoldier soldierPrefab;
    public List<AISoldier> ownedSoldiers;
    [SerializeField] private GameObject designatorPrefab;
    [SerializeField] private GameObject lookDesignatorPrefab;
    private GameObject designator;
    private GameObject lookDesignator;
    public LayerMask designatorMask;
    public LayerMask lookDesignatorMask;
    public override void OnNetworkSpawn()
    {
        designator = Instantiate(designatorPrefab, transform.position, Quaternion.identity);
        designator.SetActive(false);

        lookDesignator = Instantiate(lookDesignatorPrefab, transform.position, Quaternion.identity);
        lookDesignator.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            SpawnSoldierUmbrella();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ClearTargets();
        }
        if (Input.GetKey(KeyCode.Q)) //display target marker
        {
            ShowTarget();
        }
        if (Input.GetKeyUp(KeyCode.Q))
        { 
            CommandGoToTarget();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            FollowMe();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            ClearLookTargets();
        }
        if (Input.GetKey(KeyCode.F)) //display target marker
        {
            ShowLookTarget();
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            LookInThatDirection();
        }
    }
    private void ShowLookTarget()
    {
        lookDesignator.SetActive(true);
        Vector3 shootDirection = transform.forward;
        Ray ray = new Ray(transform.position, shootDirection);
        RaycastHit hit; //otherwise, make raycast */ 
        if (Physics.Raycast(ray, out hit, 100, lookDesignatorMask, QueryTriggerInteraction.Ignore)) //if raycast hits something  
        {
            lookDesignator.transform.position = hit.point;
        }
    }
    private void ClearLookTargets()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
            item.lookTarget = null;
        }
    }
    private void LookInThatDirection()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
            item.lookTarget = lookDesignator.transform;
        }
    }
    private void ClearTargets()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
            item.target = null;
            item.lookTarget = null;
        }
    }
    private void FollowMe()
    {
        designator.SetActive(false);
        foreach (AISoldier item in ownedSoldiers)
        {
            item.movementState = AISoldier.MovementStates.MovingToCommandedPosition;
            item.target = transform;
        }
    }
    private void ShowTarget()
    {
        designator.SetActive(true);
        Vector3 shootDirection = transform.forward;
        Ray ray = new Ray(transform.position, shootDirection);
        RaycastHit hit; //otherwise, make raycast */ 
        if (Physics.Raycast(ray, out hit, 100, designatorMask)) //if raycast hits something  
        {
            designator.transform.position = hit.point;
        }
    }
    private void CommandGoToTarget()
    { 
        //generate line formation
        foreach (AISoldier item in ownedSoldiers)
        {
            item.movementState = AISoldier.MovementStates.MovingToCommandedPosition;
            item.target = designator.transform;
        }
    }
    public Hurtbox hurtbox;
    private void SpawnSoldierUmbrella() 
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
            soldier.netObj.SpawnWithOwnership(OwnerClientId);
            soldier.hurtbox.team.Value = hurtbox.team.Value;
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
