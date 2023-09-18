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
	public enum States
    {
		SearchingForEnemies,
		FiringAtEnemies,
		Reloading, //take cover while reloading
		SearchingForAmmo, 
		TakingCover,
		RunningFromGrenade
	}
	public States state = States.SearchingForEnemies;
	 
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
	public void GetSuppressed()
    {
		Crouch();
    }
	[SerializeField] private Rigidbody body;
	private void Crouch()
    { 
		col.height = 1;
		col.center = new Vector3(0, 0.5f, 0); 
		body.AddForce(-transform.up * 10, ForceMode.Force); 
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
	[SerializeField] private Transform defaultLook;
    void Update()
	{
		if (IsOwner)
		{
			if (target != null && ai != null) ai.destination = target.position;
			if (lookTarget != null)
            { 
				RotateTowardsTransform(eyes, lookTarget, rotationSpeed); //rotate towards enemy
			}
            else
            {
				RotateTowardsTransform(eyes, defaultLook, rotationSpeed);
			}
			/*if (closestCoverPos == null)
            { 
				SearchForClosestUnoccupiedCoverPosition();
				if (target != null && ai != null) ai.destination = target;
			}
            else if (closestCoverPos.occupier == null)
			{
				ai.destination = closestCoverPos.transform.position;
			}
			else if (closestCoverPos.occupier != col)
            {
				closestCoverPos = null;
            }*/

			/*switch (state)
            {
                case States.SearchingForEnemies:
					SeekEnemy();
					break;
                case States.FiringAtEnemies:
					FireAtEnemy();
					break;
                case States.Reloading:
                    break;
                default:
                    break;
            }*/
		}
	}  
	private void SeekEnemy()
    {
		CheckIfNeedReload();
		if (focusEnemy == null)
        {
			ScanForEnemy(); 
		}
        else
        {
			state = States.FiringAtEnemies;
        } 
	}
	[SerializeField] private LayerMask coverMask;
	float searchRadius = 25;
	public CoverPosition closestCoverPos;

	[SerializeField] private CapsuleCollider col;
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
    private void OnDrawGizmos()
    {
		Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
    private void ScanForEnemy()
    {
		for (int i = -10; i <= 10; i++) //60 degrees
		{
			//once it spots an enemy, stores it as a "known enemy" and focuses on them? 
			//Physics.Raycast(transform.position, transform.forward); 
			Vector3 dir = Quaternion.AngleAxis(3 * i, eyes.up) * eyes.forward;
			bool val = Physics.Raycast(eyes.position, dir, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
			if (val && hit.collider.gameObject.layer != 3 && (hit.collider.gameObject.layer == 7 || hit.collider.gameObject.layer == 11)) //hit something other than obstacle // && hit.collider.gameObject.layer == 12
			{ 
				Debug.DrawRay(eyes.position, dir * hit.distance, Color.red);
				focusEnemy = hit.collider;
			}
			else
			{
				Debug.DrawRay(eyes.position, dir, Color.white);
			}
		}
	}
	private void CheckIfNeedReload()
	{
		if (switcher.activeWeaponType.availableAmmo <= 0)
		{
			switcher.StartReload();
		} 
	}
	private void FireAtEnemy()
    {
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
	}
	private void RotateTowardsTransform(Transform toRotate, Transform target, float rotationSpeed)
    {
		toRotate.rotation = Quaternion.LookRotation(Vector3.RotateTowards(toRotate.forward, target.position - toRotate.position, Time.deltaTime * rotationSpeed, 0));
	}
}
