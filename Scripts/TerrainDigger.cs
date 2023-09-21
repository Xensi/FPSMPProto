using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDigger : MonoBehaviour
{
    public float strength = 1;
    public int brushWidth = 1;
    public int brushHeight = 1;
    public Terrain _targetTerrain; 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        { 
            if (Physics.Raycast(transform.position, transform.forward, out var hit, Mathf.Infinity))
            {
                if (hit.transform.TryGetComponent(out Terrain terrain)) _targetTerrain = terrain; 
                LowerTerrain(hit.point, strength, brushWidth, brushHeight);
                Debug.DrawLine(transform.position, hit.point, Color.red, 2);
            }
        }
    } 

    public void LowerTerrain(Vector3 worldPosition, float strength, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] -= strength * Time.deltaTime;
            }
        }
        //may be possible to make this circular
        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
        pos = Vector3Int.FloorToInt(worldPosition);
    }
    public Vector3Int pos;
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(pos, 1);
    }
    public Vector2Int GetBrushPosition(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        var heightmapResolution = GetHeightmapResolution();

        return new Vector2Int((int)Mathf.Clamp(terrainPosition.x - brushWidth / 2.0f, 0.0f, heightmapResolution), (int)Mathf.Clamp(terrainPosition.z - brushHeight / 2.0f, 0.0f, heightmapResolution));
    }

    public Vector2Int GetSafeBrushSize(int brushX, int brushY, int brushWidth, int brushHeight)
    {
        var heightmapResolution = GetHeightmapResolution();

        while (heightmapResolution - (brushX + brushWidth) < 0) brushWidth--;

        while (heightmapResolution - (brushY + brushHeight) < 0) brushHeight--;

        return new Vector2Int(brushWidth, brushHeight);
    }
    private TerrainData GetTerrainData() => _targetTerrain.terrainData;
    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;
    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - _targetTerrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }
}
