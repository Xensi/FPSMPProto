using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ProjectileTerrainDig : NetworkBehaviour
{
    [SerializeField] private Projectile projectile;
    //public float strength = .1f;
    //public int brushWidth = 2;
    //public int brushHeight = 2; 
    public void ExplodeTerrain(Vector3 position)
    { 
        if (projectile.real)
        {
            Global.DigEvent dig = Global.Instance.AddDigEvent(position, Global.DigType.Artillery); //strength, brushWidth, brushHeight
            Global.Instance.DigClientRpc(position, Global.DigType.Artillery); //tell everybody to jot that down
        }
        //Global.Instance.Dig(dig);
    }
}
