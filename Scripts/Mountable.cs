using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Mountable : MonoBehaviour
{
    public List<Seat> seats;

    private void Update()
    {
        foreach (Seat seat in seats)
        {
            if (seat.passenger != null)
            {
                seat.passenger.position = seat.location.position;
            }
        }
    }
    public bool Interact(Transform passenger)
    {
        if (IsInVehicle(passenger))
        {
            return Dismount(passenger);
        }
        else
        {
            return Mount(passenger);
        }
    }
    private bool IsInVehicle(Transform passenger)
    {
        foreach (Seat item in seats)
        {
            if (item.passenger == passenger)
            {
                return true; 
            }
        }
        return false;
    }
    private bool Mount(Transform passenger)
    {
        //find first available seat
        foreach (Seat item in seats)
        {
            if (item.passenger == null)
            {
                item.passenger = passenger;
                return true;//in vehicle
            }
        }
        return false;
    }
    private bool Dismount(Transform passenger)
    {
        //find our seat 
        foreach (Seat item in seats)
        {
            if (item.passenger == passenger)
            {
                item.passenger = null;
                return false;
            }
        }
        return true;
    } 
} 
[Serializable]
public class Seat
{
    public Transform location;
    public Transform passenger;
}