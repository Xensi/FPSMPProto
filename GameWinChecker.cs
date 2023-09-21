using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameWinChecker : MonoBehaviour
{
    public List<CapturePoint> capPoints;
    public bool gameStarted = false;

    // Update is called once per frame
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
}
