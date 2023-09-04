using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
public class Projectile : MonoBehaviour
{
    public Rigidbody body;
    public bool damageDealt = false;
    public int damage = 1;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != 6)
        {
            if (!damageDealt)
            {
                damageDealt = true;
                Debug.Log(collision.gameObject.name);
                body.isKinematic = true;
                body.drag = Mathf.Infinity;


                if (collision.collider.TryGetComponent(out Hurtbox hurtbox))
                {
                    hurtbox.DealDamageUmbrella(damage);
                }
            }
        }
    }
}
