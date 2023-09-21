using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseDanger : MonoBehaviour
{
    [SerializeField] private AISoldier ai;
    private void OnTriggerEnter(Collider other)
    {
        /*if (other.gameObject.layer == 9)
        {
            //IncomingProjectileSensed();
        }
        else */
        if (other.gameObject.layer == 15)
        {
            GrenadeSensed(other);
        }
    }
    private void GrenadeSensed(Collider col)
    {
        Debug.Log("Grenade");
        ai.RunAwayFromGrenade(col);
    }
    public void IncomingProjectileSensed()
    { 
        ai.GetSuppressed();
    }
}
