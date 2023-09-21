using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Pathfinding;

public class AISoldier : NetworkBehaviour
{
	public NetworkObject netObj;
	public Transform target;
	public Transform lookTarget;
	IAstarAI ai;
	[HideInInspector] public SpawnSoldier spawner;
	[SerializeField] private WeaponSwitcher switcher; 
    public LayerMask mask;
	[SerializeField] private Transform eyes;
	public Collider focusEnemy;
	[SerializeField] private float rotationSpeed = 10;
	[SerializeField] private BasicShoot shooter;
	[SerializeField] private AIPath pathfinder;
	[SerializeField] private LayerMask coverMask;
	float searchRadius = 25;
	public CoverPosition closestCoverPos;

	[SerializeField] private CapsuleCollider col;
	[SerializeField] private Transform defaultLook;
	[SerializeField] private Rigidbody body;
	private float suppressionTimer = 0; 
	public Hurtbox hurtbox;
	public enum MovementStates
    {   
		TakingCover,
		RunningFromGrenade,
		MovingToCommandedPosition, //moving to commanded position, while still fighting
		ChargingForward
	}
	public MovementStates movementState = MovementStates.ChargingForward;
	public enum FiringStates
    {
		SearchingForEnemies,
		FiringAtEnemies,
		Reloading,
		FullyOutOfAmmo
	}
	public FiringStates firingState = FiringStates.SearchingForEnemies;
	 
	void OnDisable()
	{
		if (IsOwner)
		{
			if (ai != null) ai.onSearchPath -= Update; 
		}
	} 
	public enum StandStates
    {
		Standing,
		Crouching
    }
	public StandStates standState = StandStates.Standing;
    public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			ai = GetComponent<IAstarAI>();
			if (ai != null) ai.onSearchPath += Update; 
			spawner = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<SpawnSoldier>();
			spawner.ownedSoldiers.Add(this); //fix this so that soldiers are added to specific players
		}
        else //if not owner, assume is enemy
        { 
			SoldierStorage.Instance.enemySoldiers.Add(this);
		} 
	}
	public void SetLayers()
    {
		Hurtbox box = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<Hurtbox>();
		if (box.team.Value != hurtbox.team.Value)
        {
			SetLayerAllChildren(transform, 11);
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
	float centerVelocity;
	private void Crouch()
	{ 
		col.height = Mathf.SmoothDamp(col.height, 1, ref yVelocity, smoothTime);
		float newPos = Mathf.SmoothDamp(col.center.y, .5f, ref centerVelocity, smoothTime);
		col.center = new Vector3(0, newPos, 0);
		//col.height = 1;
		//col.center = new Vector3(0, 0.5f, 0);
		//body.AddForce(-transform.up * 10, ForceMode.Force);
		pathfinder.maxSpeed = fastSpeed*.5f;
	}
	float smoothTime = 0.1f;
	float yVelocity = 0.0f;
	private float fastSpeed = 8; 
	private void StandUp()
	{
		col.height = Mathf.SmoothDamp(col.height, 2, ref yVelocity, smoothTime);
		float newPos = Mathf.SmoothDamp(col.center.y, 0, ref centerVelocity, smoothTime);
		col.center = new Vector3(0, newPos, 0);
		//col.height = 2;
		//col.center = new Vector3(0, 0, 0);
		//transform.position = new Vector3(transform.position.x, transform.position.y + 1.1f, transform.position.z);
		pathfinder.maxSpeed = fastSpeed;
	}
	/// <summary>
	/// We can be suppressed by sufficient gunfire
	/// </summary>
	private void UpdateSuppression()
    {
		if (suppressionTimer > 0)
        {
			suppressionTimer = Mathf.Clamp(suppressionTimer -= Time.deltaTime, 0, 999);
			standState = StandStates.Crouching;
		} 
	} 
	public void GetSuppressed()
	{
		if (Random.Range(1, 11) <= 3)
        { 
			suppressionTimer = Mathf.Clamp(suppressionTimer += Random.Range(1, 3), 0, 5);
		}
	}
	void Update()
	{
		if (IsOwner)
		{ 
			UpdateStates();
			/*
			if (lookTarget != null)
            { 
				RotateTowardsTransform(eyes, lookTarget, rotationSpeed); //rotate towards enemy
			}
            else
            {
				RotateTowardsTransform(eyes, defaultLook, rotationSpeed);
			}*/
			/**/
		}
	}
	private void GoToUnoccupiedCover()
    {
		if (closestCoverPos == null) //we don't know of any cover
		{
			SearchForClosestUnoccupiedCoverPosition();
		}
		else if (closestCoverPos.occupier == null) //we have cover that is empty
		{
			ai.destination = closestCoverPos.transform.position;
		}
		else if (closestCoverPos.occupier != col) //cover is occupied by another
		{
			closestCoverPos = null;
		}
	}
	private void UpdateStates()
	{
        switch (movementState)
        {
            case MovementStates.TakingCover: //i'm not behind cover!
                GoToUnoccupiedCover();
                break;
            case MovementStates.RunningFromGrenade:
                GoToPosition();
                if (grenadeTimer > 0)
                {
                    grenadeTimer -= Time.deltaTime;
                }
                else
                {
                    movementState = MovementStates.TakingCover;
                }
                break;
            case MovementStates.MovingToCommandedPosition:
                FollowTransform();
                break;
            case MovementStates.ChargingForward:
				target = Global.Instance.directionOfBattle;
				FollowTransform();
                break;
            default:
                break;
        }
        switch (firingState)
        {
            case FiringStates.SearchingForEnemies: //actively scanning for enemy  
				standState = StandStates.Standing;
                CheckIfNeedReload();
                if (focusEnemy == null)
                {
                    ScanForEnemy();
					if (hurtbox.team.Value == 0)
                    {
						eyes.rotation = Quaternion.LookRotation(Vector3.RotateTowards(eyes.forward, Global.Instance.directionOfBattle.forward, Time.deltaTime * rotationSpeed, 0));
					}
                    else
                    {
						eyes.rotation = Quaternion.LookRotation(Vector3.RotateTowards(eyes.forward, -Global.Instance.directionOfBattle.forward, Time.deltaTime * rotationSpeed, 0));
					} 
				}
                else
                {
                    firingState = FiringStates.FiringAtEnemies;
                } 
                break;
            case FiringStates.FiringAtEnemies: //shooting at enemy   
				standState = StandStates.Standing;
				CheckIfNeedReload();
                if (focusEnemy != null)
                {
                    //raycast to see if enemy is still visible to us: 
                    Vector3 heading = focusEnemy.transform.position - eyes.position;
                    bool val = Physics.Raycast(eyes.position, heading, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
                    if (val && hit.collider == focusEnemy) //if still visible
                    {
                        Debug.DrawRay(eyes.position, heading * hit.distance, Color.red);
                        RotateTowardsTransform(eyes, focusEnemy.transform, rotationSpeed); //rotate towards enemy
                        if (Vector3.Dot(heading.normalized, eyes.forward.normalized) > .9f) //if pointing in direction of enemy, shoot
                        {
                            if (switcher.activeWeaponType.availableAmmo > 0)
                            {
                                if (shooter != null)
                                {
                                    shooter.AIShoot();
                                }
                            }
                        }
                    }
                    else //lost them ...
                    {
                        Debug.DrawRay(eyes.position, heading, Color.white);
                        focusEnemy = null;
                        //state = States.SearchingForEnemies;
                    }
                }
                else
                {
                    firingState = FiringStates.SearchingForEnemies;
                }
                break;
            case FiringStates.Reloading: //reloading, out of ammo 
                if (switcher.activeWeaponType.availableAmmo > 0)
                {  
					firingState = FiringStates.SearchingForEnemies;
					standState = StandStates.Standing;
				}
                else
				{
					standState = StandStates.Crouching;
				}
                break;
            case FiringStates.FullyOutOfAmmo:
                break;
            default:
                break;
        }
        
        UpdateSuppression(); 
		switch (standState)
		{
			case StandStates.Standing:
				StandUp();
				break;
			case StandStates.Crouching:
				Crouch();
				break;
			default:
				break;
		}
    }
    private float grenadeTimer = 0;
	private void SearchForClosestUnoccupiedCoverPosition()
	{
		int maxColliders = 30;
		Collider[] hitColliders = new Collider[maxColliders];
		int numColliders = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, hitColliders, coverMask, QueryTriggerInteraction.Collide);
		float dist = Mathf.Infinity;
		float newDist;
		for (int i = 0; i < numColliders; i++)
		{
			newDist = Vector3.Distance(transform.position, hitColliders[i].transform.position);
			if (newDist < dist)
			{
				if (hitColliders[i].TryGetComponent(out CoverPosition cover))
				{
					if (cover.occupier == null)
					{
						dist = newDist;
						closestCoverPos = cover;
					}
				}
			}
		}
	} 
	Vector3 destPos;
	public void RunAwayFromGrenade(Collider col)
	{
		Vector3 heading = col.transform.position - transform.position;
		destPos = -heading.normalized * 10;
		grenadeTimer = 4;
		movementState = MovementStates.RunningFromGrenade;
	}
	private void GoToPosition()
	{
		if (ai != null) ai.destination = destPos;
		Debug.DrawLine(transform.position, destPos);
	}
	private void FollowTransform()
    {  
		if (target != null && ai != null) ai.destination = target.position;
	}
    private void OnDrawGizmos()
    {
		Gizmos.DrawWireSphere(transform.position, searchRadius);
    } 
    private void ScanForEnemy()
    {
		for (int j = -1; j <= 0; j++)
        {
			for (int i = -10; i <= 10; i++) //60 degrees
			{
				//once it spots an enemy, stores it as a "known enemy" and focuses on them? 
				//Physics.Raycast(transform.position, transform.forward); 
				Vector3 dir = Quaternion.AngleAxis(3 * i, eyes.up) * eyes.forward;	
				dir = Quaternion.AngleAxis(-5 * j, eyes.right) * dir;
				bool val = Physics.Raycast(eyes.position, dir, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
				if (val && hit.collider.gameObject.layer != 3) //hit something other than obstacle // && hit.collider.gameObject.layer == 12
				{
					//is collider something with a hurtbox?
					if (hit.collider.gameObject.layer == 6 || hit.collider.gameObject.layer == 7 || hit.collider.gameObject.layer == 10 || hit.collider.gameObject.layer == 11)
                    {
						if (hit.collider.TryGetComponent(out Hurtbox box))
                        {
							if (box.team.Value != hurtbox.team.Value)
                            { 
								Debug.DrawRay(eyes.position, dir * hit.distance, Color.red);
								focusEnemy = hit.collider;
							}
						}
                    }
				}
				else
				{
					Debug.DrawRay(eyes.position, dir * 100, Color.white);
				}
			}
		} 
	}
	private void CheckIfNeedReload()
	{
		if (switcher.activeWeaponType.availableAmmo <= 0)
		{ 
			switcher.StartReload();
			firingState = FiringStates.Reloading;
		} 
	} 
	private void RotateTowardsTransform(Transform toRotate, Transform target, float rotationSpeed)
    {
		toRotate.rotation = Quaternion.LookRotation(Vector3.RotateTowards(toRotate.forward, target.position - toRotate.position, Time.deltaTime * rotationSpeed, 0));
	}
}
