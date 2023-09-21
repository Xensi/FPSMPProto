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
	private bool crouching = false;
	public enum States
    { 
		SearchingForEnemies,
		FiringAtEnemies,
		Reloading, //take cover while reloading
		SearchingForAmmo, 
		TakingCover,
		RunningFromGrenade
	}
	public States state = States.TakingCover;
	 
	void OnDisable()
	{
		if (IsOwner)
		{
			if (ai != null) ai.onSearchPath -= Update; 
		}
	} 
    public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			ai = GetComponent<IAstarAI>();
			if (ai != null) ai.onSearchPath += Update; 
			spawner = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<SpawnSoldier>();
			spawner.ownedSoldiers.Add(this);
		}
        else //if not owner, assume is enemy
        { 
			SoldierStorage.Instance.enemySoldiers.Add(this);
		}
		SetLayers();
	}
	private void SetLayers()
    {
		if (IsOwner)
        {
			//friendly
        }
        else
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
	private void Crouch()
	{
		col.height = 1;
		col.center = new Vector3(0, 0.5f, 0);
		body.AddForce(-transform.up * 10, ForceMode.Force);
		pathfinder.maxSpeed = 2;
	}
	private void StandUp()
    {
		col.height = 2;
		col.center = new Vector3(0, 0, 0);
		pathfinder.maxSpeed = 4;
	}
	private void UpdateSuppression()
    {
		if (suppressionTimer > 0)
        {
			suppressionTimer = Mathf.Clamp(suppressionTimer -= Time.deltaTime, 0, 999);
			if (!crouching)
            {
				crouching = true;
				Crouch();
			}
        }
        else
        { 
			if (crouching)
            {
				crouching = false;
				StandUp();
            }
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
	private void UpdateStates()
	{
		switch (state)
		{
			case States.SearchingForEnemies: //actively scanning for enemy 
				UpdateSuppression();
				CheckIfNeedReload();
				if (focusEnemy == null)
				{
					ScanForEnemy();
				}
				else
				{
					state = States.FiringAtEnemies;
				}
				if (closestCoverPos == null || closestCoverPos.occupier != col)
                {
					state = States.TakingCover;
                }
                break;
            case States.FiringAtEnemies: //shooting at enemy 
				UpdateSuppression();
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
					state = States.SearchingForEnemies;
				}
				break;
            case States.Reloading: //reloading, out of ammo 
                if (switcher.activeWeaponType.availableAmmo > 0)
                {
                    if (crouching)
                    {
                        crouching = false;
                        StandUp();
                    }
                    state = States.SearchingForEnemies;
                }
                else
                {
					if (!crouching)
					{
						crouching = true;
						Crouch();
					}
				}
                break;
            case States.SearchingForAmmo: //out of spare ammo
                break;
            case States.TakingCover: //i'm not behind cover!
				UpdateSuppression();
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
                else //we're in cover!
                {
					state = States.SearchingForEnemies;
                }
				break;
            case States.RunningFromGrenade: 
                GoToPosition();
                break;
            default:
                break;
        }
	}
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
		state = States.RunningFromGrenade;
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
				dir = Quaternion.AngleAxis(10 * j, eyes.right) * dir;
				bool val = Physics.Raycast(eyes.position, dir, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
				if (val && hit.collider.gameObject.layer != 3 && (hit.collider.gameObject.layer == 7 || hit.collider.gameObject.layer == 11)) //hit something other than obstacle // && hit.collider.gameObject.layer == 12
				{
					Debug.DrawRay(eyes.position, dir * hit.distance, Color.red);
					focusEnemy = hit.collider;
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
			state = States.Reloading;
		} 
	} 
	private void RotateTowardsTransform(Transform toRotate, Transform target, float rotationSpeed)
    {
		toRotate.rotation = Quaternion.LookRotation(Vector3.RotateTowards(toRotate.forward, target.position - toRotate.position, Time.deltaTime * rotationSpeed, 0));
	}
}
