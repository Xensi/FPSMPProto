using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleInteractor : MonoBehaviour
{
    public Transform playerObject;
    public KeyCode vehicleEnterKey = KeyCode.E;
    public LayerMask vehicleEntranceMask;
    private float armLength = 2;
    private bool inVehicle = false;
    private Mountable vehicleWeAreInside;
    private void InteractWithVehicle()
    {
        if (Input.GetKeyDown(vehicleEnterKey))
        {
            if (inVehicle && vehicleWeAreInside != null)
            {
                inVehicle = vehicleWeAreInside.Interact(playerObject);
                if (!inVehicle) vehicleWeAreInside = null;
            }
            else
            {
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, armLength, vehicleEntranceMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(transform.position, transform.forward * armLength);

                    if (hit.collider.TryGetComponent(out Mountable vehicle))
                    {
                        inVehicle = vehicle.Interact(playerObject);
                        if (inVehicle) vehicleWeAreInside = vehicle;
                    }
                }
                else
                {
                    Debug.DrawRay(transform.position, transform.forward * armLength, Color.red);
                }
            }
        }
        
    }
    void Update()
    {
        InteractWithVehicle(); 
    }
}
