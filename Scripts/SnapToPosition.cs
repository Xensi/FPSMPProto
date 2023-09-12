using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapToPosition : MonoBehaviour
{
    [SerializeField] private Transform center;
    [SerializeField] private Transform pos;
    void LateUpdate()
    {
        transform.position = pos.position;
        transform.LookAt(center);
    }
}
