using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverPosition : MonoBehaviour
{
    public Collider occupier;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger) occupier = other; 
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger) occupier = null;
    }
}
