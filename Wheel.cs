using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public bool touching = false;
    public float radius = 0.51f;
    public LayerMask mask;
    public float power = 1;
    public Transform newForward;

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, radius, mask, QueryTriggerInteraction.Ignore))
        {
            touching = true;
        }
        else
        {
            touching = false;
        }
    }
}
