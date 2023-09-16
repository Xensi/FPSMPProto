using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using Unity.Netcode;
public class SpawnSoldier : NetworkBehaviour
{
    [SerializeField] private AISoldier soldierPrefab;
    public List<AISoldier> ownedSoldiers;
    [SerializeField] private GameObject designatorPrefab;
    private GameObject designator;
    public LayerMask designatorMask;
    public override void OnNetworkSpawn()
    {
        if (designator == null)
        {
            designator = Instantiate(designatorPrefab, transform.position, Quaternion.identity);
            designator.SetActive(false);
        }
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
    }
    private void ClearTargets()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
            item.target = null;
        }
    }
    private void FollowMe()
    {
        foreach (AISoldier item in ownedSoldiers)
        {
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
        designator.SetActive(false);
        foreach (AISoldier item in ownedSoldiers)
        {
            item.target = designator.transform;
        }
    }
    private void SpawnSoldierUmbrella() 
    { //only server can spawn
        if (IsServer)
        {
            AISoldier soldier = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
            soldier.netObj.SpawnWithOwnership(OwnerClientId); 
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
