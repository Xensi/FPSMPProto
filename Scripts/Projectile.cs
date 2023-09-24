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
    public bool firedByPlayer = true;
    [SerializeField] private GameObject bulletHole;
    public float maxLifetime = 10;
    private void OnCollisionEnter(Collision collision)
    {
        if (!damageDealt && fuseTime == -1)
        {
            if (collision.collider.TryGetComponent(out Hurtbox hurtbox)) //effects on hitting something that can be damaged
            { 
                if (hurtbox.playerControlled)
                { 
                    if (firedByPlayer && hurtbox.OwnerClientId != id || !firedByPlayer) //either a player hit another player that isn't themselves, or it was fired by an ai
                    {
                        if (real) //only the one shot by server is real, the rest are cosmetic and don't actually deal damage
                        {
                            //Debug.Log(collision.gameObject.name);
                            hurtbox.DealDamageUmbrella(damage);
                        }
                    }
                }
                else
                {
                    if (real) //only the one shot by server is real, the rest are cosmetic and don't actually deal damage
                    {
                        //Debug.Log(collision.gameObject.name);
                        hurtbox.DealDamageUmbrella(damage);
                    }
                } 
            }
            if (bulletHole != null)
            {
                 
                ContactPoint contact = collision.GetContact(0);
                GameObject obj = Instantiate(bulletHole, transform.position, Quaternion.FromToRotation(Vector3.forward, -contact.normal));
                obj.transform.parent = collision.transform;

                /*GameObject hitParticleEffect = Instantiate(hitParticles, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal)); //create a particle effect on shot
                    GameObject bulletHole = Instantiate(bulletImpact, hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal)); //create a bullet hole on shot
                    hitParticleEffect.transform.SetParent(hit.transform); //connect to object that was hit
                    bulletHole.transform.SetParent(hit.transform);*/
            }
            damageDealt = true;
            if (stopOnImpact) body.drag = Mathf.Infinity;
            if (explodeOnImpact)
            {
                ExplodeUmbrella();
            }
        } 
    }
    private float timer = 0;
    private void Update()
    {  
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
    }
    private void ExplodeUmbrella()
    {
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
    public float explodeRadius = -1;
    public LayerMask collideMask;
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
        Destroy(gameObject);
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
                            hurtbox.DealDamageUmbrella(damage);
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
    [SerializeField] private ProjectileTerrainDig dig;
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(transform.position, explodeRadius);
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
