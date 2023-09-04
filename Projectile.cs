using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
public class Projectile : MonoBehaviour
{
    public Rigidbody body;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != 6)
        { 
            Debug.Log(collision.gameObject.name);
            Destroy(gameObject);
        }
    }
}
