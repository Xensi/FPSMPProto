using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
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
    public SpriteRenderer teamSprite;
    [SerializeField] private AdvancedPlayerMovement playerMovement;

    private int bandages = 3;
    public float bandageTimer = 0;
    private float bandageFinishTime = 3;
    public bool alive = true;

    private float maxBleedOutTime = 10;
    private float reduceBleedOutOnHit = 1;
    private float bleedOutTimer = -999;
    [SerializeField] private ParticleSystem bloodEffect;
    [SerializeField] private Slider bleedOutSlider;
    [SerializeField] private Slider bandageSlider;

    private void Start()
    { 
        if (bleedOutSlider != null)
        {
            bleedOutSlider.maxValue = maxBleedOutTime;
            bleedOutSlider.value = maxBleedOutTime;
            bleedOutSlider.gameObject.SetActive(false);
        }
        if (bandageSlider != null)
        {
            bandageSlider.maxValue = bandageFinishTime;
            bandageSlider.value = 0;
            bandageSlider.gameObject.SetActive(false);
        }

    }
    /*public void SwitchToTeam()
    { 
        LoseAllCommandableSoldiers(); 
        Respawn();
    }*/
    public SpawnSoldier spawner;
    private void LoseAllCommandableSoldiers()
    {
        if (spawner != null)
        { 
            spawner.LoseCommandOfAll();
        }
    }
    private void UpdateTeamColor()
    { 
        if (teamSprite != null)
        {
            teamSprite.color = Global.Instance.teamColors[team.Value];
        }
        if (mapSprite != null)
        {
            mapSprite.color = Global.Instance.teamColors[team.Value];
        }
    }
    public SpriteRenderer mapSprite;
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            Global.Instance.localPlayerBox = this; 
        }
        Respawn();
        //JoinTeamWithLessPeople(); 
    }
    private void JoinTeamWithLessPeople() //NOT WORKING RN
    {
        if (IsServer)
        {
            //team 0 count at 1;
            Respawn();
        }
        else
        {
            //check what team has less people
            /*byte teamWithLessPeople = 0; //0 stays on team 0
            if (Global.Instance.numTeam0.Value > Global.Instance.numTeam1.Value)
            {
                teamWithLessPeople = 1;
            }*/
            Respawn();
        } 
    }
    int oldTeam = 0;
    private void DetectTeamSwitchLocal()
    {
        if (team.Value != oldTeam)
        {
            oldTeam = team.Value;
            FinishSwitchingTeams();
        }
    }
    private void FinishSwitchingTeams()
    {
        LoseAllCommandableSoldiers();
        Respawn();
    }
    private void Respawn()
    {
        UpdateTeamColor();
        if (bloodEffect != null)
        {
            bloodEffect.Stop();
            bloodEffect.gameObject.SetActive(false);
        }
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
            ArmyBase armyBase;
            if (team.Value == 0)
            {
                armyBase = Global.Instance.base0;
            }
            else
            {
                armyBase = Global.Instance.base1;
            }
            player.transform.position = armyBase.playerSpawns[Random.Range(0, armyBase.playerSpawns.Count)].position; 
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
        }
        if (IsOwner && IsSpawned)
        { 
            UpdateBleedOut();
            DetectTeamSwitchLocal();
            if (Input.GetKey(KeyCode.Q) && bandages > 0 && bleedOutTimer != -999) //slow while healing
            {
                if (bandageTimer < bandageFinishTime)
                {
                    bandageTimer += Time.deltaTime;
                    if (bandageSlider != null)
                    {
                        bandageSlider.value = bandageTimer;
                        bandageSlider.gameObject.SetActive(true);
                    }
                    if (playerMovement != null)
                    {
                        playerMovement.moveSpeed = playerMovement.defaultSpeed / 3;
                    }
                }
                else
                {
                    bandages--;
                    bandageTimer = 0;
                    FinishApplyingBandage();
                    if (bandageSlider != null)
                    {
                        bandageSlider.value = 0;
                        bandageSlider.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                bandageTimer = 0;
                if (bandageSlider != null)
                {
                    bandageSlider.value = 0;
                    bandageSlider.gameObject.SetActive(false);
                }
                if (playerMovement != null)
                {
                    playerMovement.moveSpeed = playerMovement.defaultSpeed;
                }
            }
        }
    }
    private void FinishApplyingBandage()
    {
        bleedOutTimer = -999; 
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
            if (bleedOutSlider != null)
            {
                bleedOutSlider.maxValue = maxBleedOutTime;
                bleedOutSlider.value = bleedOutTimer;
                bleedOutSlider.gameObject.SetActive(true);
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
            if (bleedOutSlider != null)
            {
                bleedOutSlider.maxValue = maxBleedOutTime;
                bleedOutSlider.value = maxBleedOutTime;
                bleedOutSlider.gameObject.SetActive(false);
            }
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
            if (bleedOutSlider != null)
            {
                bleedOutSlider.maxValue = maxBleedOutTime;
                bleedOutSlider.value = maxBleedOutTime;
            }
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
            //Debug.Log(bleedOutTimer);
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

    public void DealDamageUmbrella(int damage)
    {
        if (alive)
        {
            float adjusted = damage / maxBleedOutTime;
            GetHitBleedOut(adjusted); 
        }
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
