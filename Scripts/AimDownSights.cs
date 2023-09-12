using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimDownSights : MonoBehaviour
{
    [SerializeField] private Transform handPositions;

    [SerializeField] private Transform basePos;
    [SerializeField] private Transform centerPos;
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            handPositions.position = centerPos.position;
        }
        else
        {
            handPositions.position = basePos.position;
        }
    }
}
