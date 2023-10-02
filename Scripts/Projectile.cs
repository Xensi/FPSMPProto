using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Pathfinding;
public class Projectile : NetworkBehaviour
{
    public Rigidbody body;
    [HideInInspector] public bool damageDealt = false;
    [HideInInspector] public int damage = 1;
    public float fuseTime = -1;
    public bool stopOnImpact = true;
    public bool explodeOnImpact = false; 
    public bool real = true;
    public ParticleSystem explosionEffect;
    public ulong id = 0;
    public int team = 0;
    public bool firedByPlayer = true;
    [SerializeField] private GameObject bulletHole;
    public float maxLifetime = 10;
    [SerializeField] private GameObject hideOnImpact;
    private float timer = 0;
    [SerializeField] private ProjectileTerrainDig dig;
    public float explodeRadius = -1;
    public LayerMask collideMask;
    [SerializeField] private float timeBeforeCollisionsAllowed = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (timeBeforeCollisionsAllowed > 0) return;
        ContactPoint contact = collision.GetContact(0);
        if (!damageDealt && fuseTime == -1) //no damage dealt and no fuse
        {
            damageDealt = true; 
            if (stopOnImpact) body.drag = Mathf.Infinity;
            if (explodeOnImpact) //impact grenade
            { 
                ExplodeUmbrella();
            }
            else //bullet
            {
                if (bulletHole != null)
                {
                    GameObject obj = Instantiate(bulletHole, transform.position, Quaternion.FromToRotation(Vector3.forward, -contact.normal));
                    obj.transform.parent = collision.transform;
                }
                if (collision.collider.TryGetComponent(out Hurtbox hurtbox)) //effects on hitting something that can be damaged
                {
                    if (firedByPlayer) //projectiles fired by players can't hurt players on own team
                    { 
                        if (hurtbox.team.Value == team && hurtbox.playerControlled) //same team and player can't be damaged
                        {

                        }
                        else
                        { 
                            if (real) //only the one shot by server is real, the rest are cosmetic and don't actually deal damage
                            {
                                hurtbox.DealDamageUmbrella(damage);
                            }
                        }
                    }
                    else if (hurtbox.team.Value != team) //projectiles fired by ai can only damage enemies
                    {
                        if (real) //only the one shot by server is real, the rest are cosmetic and don't actually deal damage
                        {
                            hurtbox.DealDamageUmbrella(damage);
                        }
                    }
                }
            } 
        }
    }   
    private void Update()
    {  
        if (timeBeforeCollisionsAllowed > 0)
        {
            timeBeforeCollisionsAllowed -= Time.deltaTime;
        }
        if (timer < maxLifetime)
        {
            timer += Time.deltaTime;
        }
        else
        {
            Destroy(gameObject);
        } 
        if (fuseTime > 0)
        {
            fuseTime -= Time.deltaTime;
        }
        else if (fuseTime != -1)
        {
            ExplodeUmbrella();
        }
        RotateTowardsMovement();
    }
    private bool hasExploded = false;
    private void RotateTowardsMovement()
    {
        Vector3 dir = body.velocity.normalized;
        float speed = body.velocity.magnitude;
        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, dir, speed * Time.deltaTime, 0));
    }
    private void ExplodeUmbrella()
    {
        if (!hasExploded)
        {
            hasExploded = true;

            Vector3 position = transform.position;
            BaseExplode(position);
            if (IsServer)
            {
                ExplodeClientRpc(position);
            }
            else
            {
                ExplodeServerRpc(position);
            }
        }
    }
    public bool destroyOnExplosion = false;
    private void BaseExplode(Vector3 position)
    { 
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, position, Quaternion.identity);
        }
        if (real)
        {
            DealExplosionDamage(position);
        }
        if (dig != null) dig.ExplodeTerrain(position);
        //Destroy(gameObject);
        if (hideOnImpact != null) hideOnImpact.SetActive(false);
        if (destroyOnExplosion)
        {
            Destroy(gameObject);
        }
    }
    private void DealExplosionDamage(Vector3 position)
    {
        int maxColliders = 20;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(position, explodeRadius, hitColliders);
        for (int i = 0; i < numColliders; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Ray ray = new(position + new Vector3(0, 0.5f, 0), (hitColliders[i].transform.position + hitColliders[i].transform.up * 0.75f * j) - position);
                if (Physics.Raycast(ray, out RaycastHit hit, explodeRadius, collideMask, QueryTriggerInteraction.Ignore))
                {
                    //if we hit the collider
                    if (hit.collider == hitColliders[i])
                    {
                        if (hitColliders[i].TryGetComponent(out Hurtbox hurtbox))
                        {
                            if (hurtbox.soldier != null && hurtbox.soldier.body != null)
                            {
                                hurtbox.soldier.body.AddExplosionForce(500, position, explodeRadius);
                            }
                            //Debug.DrawLine(position, hit.point, Color.red, 10);
                            //calculate damage
                            float modifier = hit.distance / explodeRadius;
                            int distanceDamage = Mathf.Clamp(Mathf.RoundToInt(damage - (damage * modifier)), 0, 999);
                            //Debug.Log(distanceDamage);
                            hurtbox.DealDamageUmbrella(distanceDamage);
                        }
                        break;
                    }
                    else
                    {
                        Debug.DrawLine(position, hit.point, Color.white, 10);
                    }
                }
            }
        }
    }  
    [ClientRpc]
    private void ExplodeClientRpc(Vector3 position)
    {
        if (!IsOwner)
        {
            BaseExplode(position);
        }
    }
    [ServerRpc (RequireOwnership = false)]
    private void ExplodeServerRpc(Vector3 position)
    {
        ExplodeClientRpc(position);
    }
}
