using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DrivableVehicle : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private List<Wheel> wheels;

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        { 
            foreach (Wheel item in wheels)
            {
                if (item.touching)
                {
                    body.AddForceAtPosition(item.power * item.newForward.forward, item.transform.position, ForceMode.Force);
                }
            }
        }
    }
} 