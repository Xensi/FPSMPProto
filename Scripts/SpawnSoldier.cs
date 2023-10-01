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
    private ArmyBase ourBase;
    //player should know what soldiers they have recruited
    public List<AISoldier> commandableSoldiers;
    public override void OnNetworkSpawn()
    {
        designator = Instantiate(designatorPrefab, transform.position, Quaternion.identity);
        designator.SetActive(false);

        lookDesignator = Instantiate(lookDesignatorPrefab, transform.position, Quaternion.identity);
        lookDesignator.SetActive(false);

        if (hurtbox.team.Value == 0)
        {
            ourBase = Global.Instance.base0;
        }
        else
        {
            ourBase = Global.Instance.base1;
        }
        RetreatCommand();
    }
    public void LoseCommandOfAll()
    {
        commandableSoldiers.Clear();
    }
    private void OpenMap()
    {
        mapOpen = true;
        Global.Instance.mapCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Confined;
    }
    private void CloseMap()
    {
        mapOpen = false;
        Global.Instance.mapCamera.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public bool mapOpen = false;
    private Vector3 firePosition;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(firePosition, 30);
    }
    private Vector3 movePosition;
    private void Update()
    { 
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (mapOpen)
            {
                CloseMap();
            }
            else
            {
                OpenMap(); 
            }
        } 
        if (mapOpen)
        {
            if (Input.GetMouseButtonDown(0))
            { 
                RaycastHit hit;
                Ray ray = Global.Instance.mapCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    firePosition = hit.point;
                    CommandArtilleryFirePoint(firePosition);
                }
            }
            if (Input.GetMouseButtonDown(1)) //temporary, make it so that it moves only selected units, and fix control scheme 
            {
                RaycastHit hit;
                Ray ray = Global.Instance.mapCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    movePosition = hit.point;
                    CommandMoveToPosition();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ClearTargets();
        }
        if (Input.GetKey(KeyCode.E)) //display target marker
        {
            ShowTarget();
        }
        if (Input.GetKeyUp(KeyCode.E))
        { 
            CommandGoToTarget();
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            ChargeCommand();
        }
        if (Input.GetKeyUp(KeyCode.F))
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
        //RepeatCommand();
    }
    public Vector3 lastDestination;
    private void RepeatCommand()
    { 
        SoldiersGoToDestination(lastDestination);
    }
    private void SpreadOut()
    {

    }
    private void ChargeCommand()
    { 
        Transform chargeTowards;
        float offset = 1;
        float offsetAmount = 100 * 1f;
        if (hurtbox.team.Value == 0)
        {  
            chargeTowards = Global.Instance.base1.transform;
            offset = -1;
        }
        else
        { 
            chargeTowards = Global.Instance.base0.transform;
        }
        Vector3 destination = chargeTowards.transform.position + new Vector3(0, 0, offsetAmount * offset);
        lastDestination = destination;
        //SoldiersGoToDestination(destination);
    }
    private void SoldiersGoToDestination(Vector3 destination)
    {
        CullDeadSoldiers();
        //generate line formation
        int x = 0;
        int _unitWidth = commandableSoldiers.Count;
        var middleOffset = new Vector3(_unitWidth * 0.5f * spread, 0, 0);
        foreach (AISoldier item in commandableSoldiers)
        {
            item.movementState = AISoldier.MovementStates.MovingToCommandedPosition;
            Vector3 pos = destination;
            pos += new Vector3(x * spread, 0, 0);
            pos -= middleOffset;
            item.destPos = pos;
            x++;
        }
    } 
    public void CullDeadSoldiers()
    {
        //clear empty spots

        for (int i = commandableSoldiers.Count - 1; i >= 0; i--)
        {
            if (!commandableSoldiers[i].hurtbox.alive)
            {
                commandableSoldiers.RemoveAt(i);
            }
        }
    }
    private void RetreatCommand()
    {  
        float offset = 1;
        float offsetAmount = 100 * 0.45f;
        if (hurtbox.team.Value == 0)
        { 
            offset = 1;
        }
        else
        { 
            offset = -1;
        }
        Vector3 destination = ourBase.transform.position + new Vector3(0, 0, offsetAmount * offset);
        lastDestination = destination;
        //SoldiersGoToDestination(destination);
    }
    private void CommandMoveToPosition()
    {
        lastDestination = movePosition;
        SoldiersGoToDestination(lastDestination);
    }
    private void CommandGoToTarget()
    {
        lastDestination = designator.transform.position;
        SoldiersGoToDestination(designator.transform.position);
    }
    private void CommandArtilleryFirePoint(Vector3 firePoint)
    { 
        CullDeadSoldiers();  
        foreach (AISoldier item in commandableSoldiers)
        {
            if (item.soldierType == AISoldier.SoldierTypes.Artillery)
            { 
                item.firingState = AISoldier.FiringStates.FiringAtLocation;
                item.positionToFireAt = firePoint;
            }
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
