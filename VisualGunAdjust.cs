using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualGunAdjust : MonoBehaviour
{
    [SerializeField] private Transform angle;
    [SerializeField] private Transform basePos;
    [SerializeField] private Transform acutePos;
    [SerializeField] private Transform highPos;
    [SerializeField] private Transform jumpPos;
    [SerializeField] private Jolt jolt;
    [SerializeField] private PlayerMovement movement;

    private float t = 0;
    private float x = 0;
    private float f = 0;
    private void Update()
    {
        switch (movement.playerMovementState)
        {
            case PlayerMovement.MovementStates.Idle: 
            case PlayerMovement.MovementStates.Walking:  
            case PlayerMovement.MovementStates.Jumping:
                x = angle.eulerAngles.x;
                if (x >= 0 && x <= 90)
                {
                    t = x / 90;
                    transform.position = Vector3.Lerp(basePos.position, acutePos.position, t);
                }
                else
                {
                    f = x - 360;
                    t = f / -90;
                    transform.position = Vector3.Lerp(basePos.position, highPos.position, t);
                }
                break;
            default:
                break;
        }
    }

}
