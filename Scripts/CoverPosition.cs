using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoverPosition : MonoBehaviour
{
    public Collider occupier;
    public LayerMask terrainMask;
    public LayerMask occupierMask;
    private void Start()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
    }
    private bool LayerInMask(int checkLayer, LayerMask maskAgainst)
    {
        int checkLayerMask = 1 << checkLayer;
        return (maskAgainst & checkLayerMask) != 0;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (LayerInMask(other.gameObject.layer, occupierMask))
        { 
            if (!other.isTrigger) occupier = other;
        }
        else if (other.isTrigger) //could be another cover position that is too close
        {
            if (other.TryGetComponent(out CoverPosition cover))
            {
                Destroy(cover.gameObject); //KILL THEM
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (LayerInMask(other.gameObject.layer, occupierMask))
        {
            if (!other.isTrigger) occupier = null;
        }
    }
}
