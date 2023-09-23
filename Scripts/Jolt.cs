using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jolt : MonoBehaviour
{
    private Vector3 currentRotation;
    private Vector3 targetRotation;
     
    private Vector3 targetPos;
    public Vector3 defaultPos;
    public Vector3 push; //make negative to push towards you

    public float joltX = -2; //up/down rotation
    public float joltY = 2;
    public float joltZ = 0.35f;
    public float joltSnapSpeed = 6; //speed at which moves to new position
    public float joltReturnSpeed = 100f; //speed at which returns to base position
    private void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, joltReturnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, joltSnapSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);


        targetPos = Vector3.Lerp(targetPos, defaultPos, joltReturnSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, joltSnapSpeed * Time.deltaTime);
        
    } 
    public void FireJolt()
    {
        targetRotation += new Vector3(joltX, Random.Range(-joltY, joltY), Random.Range(-joltZ, joltZ));
        targetPos += push;
    }
}
