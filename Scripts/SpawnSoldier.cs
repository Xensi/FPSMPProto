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
    public Hurtbox hurtbox;
    public float spread = 3;
    public override void OnNetworkSpawn()
    {
        designator = Instantiate(designatorPrefab, transform.position, Quaternion.identity);
        designator.SetActive(false);

        lookDesignator = Instantiate(lookDesignatorPrefab, transform.position, Quaternion.identity);
        lookDesignator.SetActive(false);
    }
    private void Update()
    { 
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
        if (Input.GetKeyUp(KeyCode.C))
        {
            ChargeCommand();
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            RetreatCommand();
        }
        /*if (Input.GetKeyDown(KeyCode.E))
        {
            FollowMe();
        }*/
        /*if (Input.GetKeyDown(KeyCode.F))
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
        }*/
    }
    private Vector3 lastDestination;
    private void SpreadOut()
    {

    }
    private void ChargeCommand()
    {
        ArmyBase armyBase; // to charge towards
        Transform chargeTowards;
        float offset = 1;
        float offsetAmount = 100 * 1f;
        if (hurtbox.team.Value == 0)
        {
            armyBase = Global.Instance.base0;
            //chargeTowards = GameWinChecker.Instance.nextCapZone0;
            chargeTowards = Global.Instance.base1.transform;
            offset = -1;
        }
        else
        {
            armyBase = Global.Instance.base1;
            //chargeTowards = GameWinChecker.Instance.nextCapZone1;
            chargeTowards = Global.Instance.base0.transform;
        }
        Vector3 destination = chargeTowards.transform.position + new Vector3(0, 0, offsetAmount * offset);
        SoldiersGoToDestination(armyBase, destination);
    }
    private void SoldiersGoToDestination(ArmyBase ourBase, Vector3 destination)
    {
        ourBase.CullDeadSoldiers();
        //generate line formation
        int x = 0;
        int _unitWidth = ourBase.spawnedSoldiers.Count;
        var middleOffset = new Vector3(_unitWidth * 0.5f * spread, 0, 0);
        foreach (AISoldier item in ourBase.spawnedSoldiers)
        {
            item.movementState = AISoldier.MovementStates.MovingToCommandedPosition;
            Vector3 pos = destination;
            pos += new Vector3(x * spread, 0, 0);
            pos -= middleOffset;
            item.destPos = pos;
            x++;
        }
        lastDestination = destination;
    }
    private void RetreatCommand()
    { 
        ArmyBase armyBase; // to charge towards 
        float offset = 1;
        float offsetAmount = 100 * 0.45f;
        if (hurtbox.team.Value == 0)
        {
            armyBase = Global.Instance.base0; 
            offset = 1;
        }
        else
        {
            armyBase = Global.Instance.base1;
            offset = -1;
        }
        Vector3 destination = armyBase.transform.position + new Vector3(0, 0, offsetAmount * offset);
        SoldiersGoToDestination(armyBase, destination);
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
        ArmyBase armyBase;
        if (hurtbox.team.Value == 0)
        {
            armyBase = Global.Instance.base0;
        }
        else
        {
            armyBase = Global.Instance.base1;
        }
        SoldiersGoToDestination(armyBase, designator.transform.position);
    } 
    public Vector3 GetNoise(Vector3 pos)
    {
        float _noise = 0.5f;
        var noise = Mathf.PerlinNoise(pos.x * _noise, pos.z * _noise);

        return new Vector3(noise, 0, noise);
    }
    /*private void SpawnSoldierUmbrella() 
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
    }  */
}
