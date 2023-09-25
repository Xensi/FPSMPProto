using System.Collections;
using System.Collections.Generic;
using System;
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
	public AIPath pathfinder;
	[SerializeField] private LayerMask coverMask;
	[SerializeField] private LayerMask obstacleMask;
	float searchRadius = 25;
	public CoverPosition closestCoverPos;

	[SerializeField] private CapsuleCollider col;
	[SerializeField] private Transform defaultLook;
	public Rigidbody body;
	private float suppressionTimer = 0; 
	public Hurtbox hurtbox;
	public enum MovementStates
    {   
		TakingCover,
		RunningFromGrenade,
		MovingToCommandedPosition, //moving to commanded position, while still fighting 
		HoldingPosition //for static emplacement soldiers
	}
	public MovementStates movementState = MovementStates.HoldingPosition;
	public enum FiringStates
    {
		SearchingForEnemies,
		FiringAtEnemies,
		Reloading,
		FullyOutOfAmmo,
		AwaitingFiringLocation, //artillery
		FiringAtLocation //artillery
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
			//soldiers do not start out owned by anyone? save for player's starting retinue
			//spawner = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<SpawnSoldier>();
			//spawner.ownedSoldiers.Add(this); //fix this so that soldiers are added to specific players
		}
        /*else //if not owner, assume is enemy
        { 
			SoldierStorage.Instance.enemySoldiers.Add(this);
		} */
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
			//Debug.Log(child.name);
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
		if (UnityEngine.Random.Range(1, 11) <= 3)
        { 
			suppressionTimer = Mathf.Clamp(suppressionTimer += UnityEngine.Random.Range(1, 3), 0, 5);
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
	public ThrowData throwData;  
	private void CheckIfWayForwardBlocked()
    {
		if (Physics.Raycast(transform.position + new Vector3(0, 1, 0), transform.forward, out RaycastHit hit, 1, obstacleMask, QueryTriggerInteraction.Ignore))
		{
			body.useGravity = false;
			body.AddForce(transform.up * 5, ForceMode.Force);
			Debug.DrawLine(transform.position, hit.point, Color.red);
		}
		else
        {
			body.useGravity = true;
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
				CheckIfWayForwardBlocked();

				GoToPosition();
                break; 
            case MovementStates.HoldingPosition:
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
					Vector3 dir;
					if (hurtbox.team.Value == 0)
                    {
						dir = Global.Instance.directionOfBattle.forward;
					}
                    else
					{
						dir = -Global.Instance.directionOfBattle.forward;
					}
					RotateTowardsDirection(eyes, dir, rotationSpeed);
				}
                else
                {
                    firingState = FiringStates.FiringAtEnemies;
                } 
                break;
            case FiringStates.FiringAtEnemies: //shooting at enemy    
				standState = StandStates.Standing;
				CheckIfNeedReload(); 
                if (focusEnemy != null && enemyHurtbox != null && enemyHurtbox.alive && enemyHurtbox.team.Value != hurtbox.team.Value)
				{
					Vector3 heading = focusEnemy.transform.position - eyes.position;
					if (switcher.activeWeaponType.data.aiIndirectFire)
                    {
						RotateTowardsTransform(eyes, focusEnemy.transform, rotationSpeed); //rotate towards enemy
						if (Vector3.Dot(heading.normalized, eyes.forward.normalized) >= .99f) //if eyes is pointing in direction of enemy
						{ 
							throwData = CalculateFiringAngle(focusEnemy.transform.position, shooter.muzzle.transform.position, switcher.activeWeaponType.data.force, switcher.activeWeaponType.data.aiRatio);
							if (throwData.valid)
							{ 
								if (weaponParent != null)
								{
									//Vector3 angleVector = Quaternion.AngleAxis(throwData.Angle, weaponParent.right) * eyes.forward.normalized;
									Vector3 angleVector = throwData.ThrowVelocity.normalized;
									shooter.force = throwData.ThrowVelocity.magnitude;
									Debug.DrawRay(transform.position, angleVector);
									RotateTowardsDirection(weaponParent, angleVector, rotationSpeed);

									if (Vector3.Dot(angleVector, weaponParent.forward.normalized) >= .99f) //if weapon matches angle
									{  
										TryToShoot();
									}
								}
							}
						}
					}
                    else
                    {
						//raycast to see if enemy is still visible to us: 
						bool val = Physics.Raycast(eyes.position, heading, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
						if (val && hit.collider == focusEnemy) //if still visible
						{
							//Debug.DrawRay(eyes.position, heading * hit.distance, Color.red);
							RotateTowardsTransform(eyes, focusEnemy.transform, rotationSpeed); //rotate towards enemy
							if (Vector3.Dot(heading.normalized, eyes.forward.normalized) > .9f) //if pointing in direction of enemy, shoot
							{
								TryToShoot();
							}
						}
						else //lost them ...
						{
							Debug.DrawRay(eyes.position, heading, Color.white);
							focusEnemy = null;
							//state = States.SearchingForEnemies;
						}
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
        
        //UpdateSuppression(); 
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
	/// <summary>
	/// Throw Velocity, Angle
	/// </summary>
	[Serializable]
	public struct ThrowData
	{
		public bool valid;
		public Vector3 ThrowVelocity;
		public float Angle;
		public float DeltaXZ;
		public float DeltaY;
	}
	public Hurtbox enemyHurtbox;
	public ThrowData CalculateFiringAngle(Vector3 TargetPosition, Vector3 StartPosition, float MaxThrowForce, float ForceRatio = 1) //force ratio 0 is lowest possible force, 1 is max
	{
		// v = initial velocity, assume max speed for now
		// x = distance to travel on X/Z plane only
		// y = difference in altitudes from thrown point to target hit point
		// g = gravity

		Vector3 displacement = new Vector3(
			TargetPosition.x,
			StartPosition.y,
			TargetPosition.z
		) - StartPosition;
		float deltaY = TargetPosition.y - StartPosition.y;
		float deltaXZ = displacement.magnitude;

		// find lowest initial launch velocity with other magic formula from https://en.wikipedia.org/wiki/Projectile_motion
		// v^2 / g = y + sqrt(y^2 + x^2)
		// meaning.... v = sqrt(g * (y+ sqrt(y^2 + x^2)))
		float gravity = Mathf.Abs(Physics.gravity.y);
		float throwStrength = Mathf.Clamp(
			Mathf.Sqrt(
				gravity
				* (deltaY + Mathf.Sqrt(Mathf.Pow(deltaY, 2)
				+ Mathf.Pow(deltaXZ, 2)))),
			0.01f,
			MaxThrowForce
		);
		throwStrength = Mathf.Lerp(throwStrength, MaxThrowForce, ForceRatio);

		float angle;
		if (ForceRatio == 0)
		{
			// optimal angle is chosen with a relatively simple formula
			angle = Mathf.PI / 2f - (0.5f * (Mathf.PI / 2 - (deltaY / deltaXZ)));
		}
		else
		{
			// when we know the initial velocity, we have to calculate it with this formula
			// Angle to throw = arctan((v^2 +- sqrt(v^4 - g * (g * x^2 + 2 * y * v^2)) / g*x)
			angle = Mathf.Atan(
				(Mathf.Pow(throwStrength, 2) - Mathf.Sqrt(
					Mathf.Pow(throwStrength, 4) - gravity
					* (gravity * Mathf.Pow(deltaXZ, 2)
					+ 2 * deltaY * Mathf.Pow(throwStrength, 2)))
				) / (gravity * deltaXZ)
			);
		}

		if (float.IsNaN(angle))
		{
			// you will need to handle this case when there
			// is no feasible angle to throw the object and reach the target.
			return new ThrowData
			{
				valid = false
			};
		}

		Vector3 initialVelocity =
			Mathf.Cos(angle) * throwStrength * displacement.normalized
			+ Mathf.Sin(angle) * throwStrength * Vector3.up;

		return new ThrowData
		{
			valid = true,
			ThrowVelocity = initialVelocity,
			Angle = angle,
			DeltaXZ = deltaXZ,
			DeltaY = deltaY
		};
	}
	private void TryToShoot()
    {
		if (switcher.activeWeaponType.availableAmmo > 0)
		{
			if (shooter != null)
			{ 
				shooter.AIShoot();
			}
		}
	}
	[SerializeField] private Transform weaponParent;
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
	public Vector3 destPos;
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
	public AISoldier focusEnemySoldier;
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
							if (box.team.Value != hurtbox.team.Value && box.alive)
                            { 
								//Debug.DrawRay(eyes.position, dir * hit.distance, Color.red);
								focusEnemy = hit.collider;
								enemyHurtbox = box;
							}
						}
                    }
				}
				else
				{
					//Debug.DrawRay(eyes.position, dir * 100, Color.white);
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
	private void RotateTowardsDirection(Transform toRotate, Vector3 direction, float rotationSpeed)
	{
		toRotate.rotation = Quaternion.LookRotation(Vector3.RotateTowards(toRotate.forward, direction, Time.deltaTime * rotationSpeed, 0)); 
	}
}
