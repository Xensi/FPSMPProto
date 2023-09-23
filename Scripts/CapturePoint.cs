using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class CapturePoint : NetworkBehaviour
{
    public NetworkVariable<int> holdingTeam = new();
    public int startingTeam = 0;

    public float captureProgress = 0; //switch team at 100. min 0
    public int balanceOfPower = 0;
    public List<Hurtbox> presentSoldiers;
    //remove null soldiers
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            holdingTeam.Value = startingTeam;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Hurtbox box))
        {
            presentSoldiers.Add(box);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Hurtbox box))
        {
            presentSoldiers.Remove(box);
        }
    }
    private void Update()
    {
        //if an enemy soldier is in this zone, they contribute to capturing this zone positively.
        //friendly soldiers (same team as zone) contribute negatively

        captureProgress = Mathf.Clamp(captureProgress += Time.deltaTime * balanceOfPower, 0, 100);
        if (captureProgress >= 100)
        {
            captureProgress = 0;
            SwitchTeam();
        }
        EvaluateBalanceOfPower();
    }
    private void EvaluateBalanceOfPower()
    {
        balanceOfPower = 0; 
        for (int i = presentSoldiers.Count - 1; i >= 0; i--)
        { 
            if (presentSoldiers[i] == null || !presentSoldiers[i].alive)
            {
                presentSoldiers.RemoveAt(i);
            }
            else
            { 
                int team = presentSoldiers[i].team.Value;
                if (team == holdingTeam.Value)
                {
                    balanceOfPower -= 1;
                }
                else
                {
                    balanceOfPower += 1;
                }
            }
        }
        spriteRenderer.color = colors[holdingTeam.Value];
    }
    public List<Color> colors;
    public SpriteRenderer spriteRenderer;
    private void SwitchTeam()
    {
        if (holdingTeam.Value == 0)
        {
            holdingTeam.Value = 1;
        }
        else
        {
            holdingTeam.Value = 0;
        }
    }


}
