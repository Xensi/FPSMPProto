using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global : MonoBehaviour
{
    public static Global Instance { get; private set; }
    public Terrain terrain;
    public Transform directionOfBattle; 
    private void Awake()
    {  
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
}
