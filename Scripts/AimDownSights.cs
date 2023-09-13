using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimDownSights : MonoBehaviour
{
    [SerializeField] private Transform handPositions;

    [SerializeField] private Transform basePos;
    [SerializeField] private Transform centerPos;
    public float smoothTime = 0.3F;
    private Vector3 velocity = Vector3.zero;
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            handPositions.position = Vector3.SmoothDamp(handPositions.position, centerPos.position, ref velocity, smoothTime); 
        }
        else
        {
            handPositions.position = Vector3.SmoothDamp(handPositions.position, basePos.position, ref velocity, smoothTime);
        }
    }
}
