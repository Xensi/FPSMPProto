using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class Projectile : NetworkBehaviour
{
    public Rigidbody body;
    [HideInInspector] public bool damageDealt = false;
    [HideInInspector] public int damage = 1;
    public float fuseTime = -1;
    public bool stopOnImpact = true;
    public bool explodeOnImpact = false;
    private int explosionDamage = 100;
    [HideInInspector] public bool real = true;
    public ParticleSystem explosionEffect;
    private void OnCollisionEnter(Collision collision)
    {
        if (!damageDealt && fuseTime == -1)
        {
            if (collision.gameObject.layer != 6 && collision.gameObject.layer != 9)
            {
                damageDealt = true;
                Debug.Log(collision.gameObject.name);
                if (stopOnImpact) body.drag = Mathf.Infinity;

                if (real) //only the one shot by server is real, the rest are cosmetic and don't actually deal damage
                { 
                    if (collision.collider.TryGetComponent(out Hurtbox hurtbox))
                    {
                        hurtbox.DealDamageUmbrella(damage);
                    }
                }
            }
        } 
    }
    private void Update()
    {
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
        if (IsServer)
        {
            ExplodeClientRpc();
        }
        else
        {
            ExplodeServerRpc();
        }
        BaseExplode(); 
    }
    public float explodeRadius = -1;
    private void BaseExplode()
    { 
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        if (real)
        {
            int maxColliders = 20;
            Collider[] hitColliders = new Collider[maxColliders];
            int numColliders = Physics.OverlapSphereNonAlloc(transform.position, explodeRadius, hitColliders);
            for (int i = 0; i < numColliders; i++)
            { 
                if (hitColliders[i].TryGetComponent(out Hurtbox hurtbox))
                {
                    hurtbox.DealDamageUmbrella(damage);
                }
            } 
        } 
        Destroy(gameObject);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }

    [ClientRpc]
    private void ExplodeClientRpc()
    {
        if (!IsOwner)
        {
            BaseExplode();
        }
    }
    [ServerRpc]
    private void ExplodeServerRpc()
    {
        ExplodeClientRpc();
    }
}
