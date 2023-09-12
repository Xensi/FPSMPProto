using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierStorage : MonoBehaviour
{
    public static SoldierStorage Instance { get; private set; }
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
    public List<AISoldier> enemySoldiers;
}
