using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameWinChecker : MonoBehaviour
{
    public List<CapturePoint> capPoints;
    public bool gameStarted = false;
    public static GameWinChecker Instance { get; private set; }

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
    void Update()
    {
        if (gameStarted)
        { 
            int check = 0;
            foreach (CapturePoint item in capPoints)
            {
                if (item.holdingTeam.Value == 0)
                {
                    check += 1;
                }
                else
                {
                    check -= 1;
                }
            }
            if (Mathf.Abs(check) == capPoints.Count)
            {
                Debug.Log("Someone won");
                gameStarted = false;
            }  
        } 
    }
    private void Start()
    {
        InvokeRepeating(nameof(DefineNextCapZone), 0, 1);
    }

    public Transform nextCapZone0;
    public Transform nextCapZone1; 
    private void DefineNextCapZone()
    {
        //for team 0, next capture point is a point that doesn't match their team
        foreach (CapturePoint item in capPoints)
        { 
            if (item.holdingTeam.Value != 0)
            {
                nextCapZone0 = item.transform;
                break;
            }
        }
        for (int i = capPoints.Count-1; i >= 0; i--)
        {
            if (capPoints[i].holdingTeam.Value != 1)
            {
                nextCapZone1 = capPoints[i].transform;
                break;
            }
        }
    }
}
