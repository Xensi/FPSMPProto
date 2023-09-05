using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWeapon : MonoBehaviour
{ 
    public Transform baseTransform;
    public Transform chargedTransform;
    [SerializeField] private BasicShoot shoot;

    private void Update()
    {
        transform.position = Vector3.Lerp(baseTransform.position, chargedTransform.position, shoot.chargedFloat / shoot.chargedFloatCap);
    }
}
