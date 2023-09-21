using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainRuntime : MonoBehaviour
{
    public Terrain terrain;
    private void Start()
    {
        terrain.terrainData = TerrainDataCloner.Clone(terrain.terrainData);
        terrain.GetComponent<TerrainCollider>().terrainData = terrain.terrainData; // Don't forget to update the TerrainCollider as well 
    }
}
