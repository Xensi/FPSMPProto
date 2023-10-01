using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFace : MonoBehaviour
{
    [SerializeField] private Hurtbox toMonitor;
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.up);
        if (toMonitor != null)
        {
            if (!toMonitor.alive) Destroy(gameObject); //just destroy this if dead
        }
    }
}
