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

        }
    }
    public void IncomingProjectileSensed()
    {
        Debug.Log("Scared");
        ai.GetSuppressed();
    }
}
