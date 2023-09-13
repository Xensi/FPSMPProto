using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Pathfinding;

public class AISoldier : NetworkBehaviour
{
    public NetworkObject netObj;
	public Vector3 target;
	IAstarAI ai;
	[HideInInspector] public SpawnSoldier spawner;
    public LayerMask mask;
	 
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
			target = transform.position;
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

        }
        else
        {

        }
    }
	[SerializeField] private Transform eyes;
	public Collider focusEnemy;
	[SerializeField] private float rotationSpeed = 10;
	[SerializeField] private BasicShoot shooter;
    void Update()
	{
		if (IsOwner)
        { 
			if (target != null && ai != null) ai.destination = target;
			SeekEnemy();
        }
	}  
	private void SeekEnemy()
    { 
		if (focusEnemy == null)
        { 
			for (int i = -10; i <= 10; i++) //60 degrees
			{
				//once it spots an enemy, stores it as a "known enemy" and focuses on them? 
				//Physics.Raycast(transform.position, transform.forward); 
				Vector3 dir = Quaternion.AngleAxis(3 * i, eyes.up) * eyes.forward;
				bool val = Physics.Raycast(eyes.position, dir, out RaycastHit hit, Mathf.Infinity, mask, QueryTriggerInteraction.Ignore);
				if (val && hit.collider.gameObject.layer != 3) //hit something other than obstacle
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
        else
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
					if (shooter != null) shooter.AIShoot();
				}
			}
            else //lost them ...
			{
				Debug.DrawRay(eyes.position, heading, Color.white);
				focusEnemy = null;
            }
		}
	}
	private void RotateTowardsTransform(Transform toRotate, Transform target, float rotationSpeed)
    {
		toRotate.rotation = Quaternion.LookRotation(Vector3.RotateTowards(toRotate.forward, target.position - toRotate.position, Time.deltaTime * rotationSpeed, 0));
	}
}
