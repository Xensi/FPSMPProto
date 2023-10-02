using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleInteractor : MonoBehaviour
{
    public Transform playerObject;
    public KeyCode vehicleEnterKey = KeyCode.E;
    public LayerMask vehicleEntranceMask;
    private float armLength = 2;
    private void InteractWithVehicle()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, armLength, vehicleEntranceMask, QueryTriggerInteraction.Collide))
        {
            Debug.DrawRay(transform.position, transform.forward * armLength);
            if (Input.GetKeyDown(vehicleEnterKey))
            {
                if (hit.collider.TryGetComponent(out Mountable vehicle))
                {
                    playerObject.transform.parent = vehicle.seats[0].transform;
                    Debug.Log("Yep");
                }
            }
        }
        else
        { 
            Debug.DrawRay(transform.position, transform.forward * armLength, Color.red);
        }
    }
    void Update()
    {
        InteractWithVehicle(); 
    }
}
