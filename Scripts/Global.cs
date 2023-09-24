using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Netcode;
public class Global : NetworkBehaviour
{
    public static Global Instance { get; private set; }
    public Terrain terrain;
    public Transform directionOfBattle;
    public ArmyBase base0;
    public ArmyBase base1;
    //list of terrain dig events to be sent to joining clients, held only serverside
    public List<DigEvent> digEvents; 
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
    private void Update()
    { 
    }
    public override void OnNetworkSpawn()
    {
    }
    [ClientRpc]
    public void DigClientRpc(Vector3 position, DigType type)
    {
        DigEvent dig = new();
        dig.position = position;
        dig.type = type;
        Instance.Dig(dig);
    }
    public DigEvent AddDigEvent(Vector3 position, DigType type)
    {
        DigEvent dig = new();
        dig.position = position;
        dig.type = type;
 /*       dig.strength = strength;
        dig.brushWidth = width;
        dig.brushHeight = height;*/
        digEvents.Add(dig);
        return dig;
    }
    [Serializable]
    public struct DigEvent
    {
        public Vector3 position;
        public DigType type;
        /*public float strength;
        public int brushWidth;
        public int brushHeight;*/
    }
    public enum DigType : byte
    {
        Artillery,
        Trench
    }

    public void Dig (DigEvent dig) //DigType type, Vector3 position
    {
        switch (dig.type)
        {
            case DigType.Artillery:
                Debug.LogError("Digging");
                LowerTerrain(dig.position, .001f, 2, 2);
                break;
            case DigType.Trench:
                Debug.LogError("not implemented");
                //LowerTerrain(position, .001f, 2, 2);
                break;
            default:
                break;
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
                heights[y, x] -= strength;// * Time.deltaTime;
                Debug.Log(heights[y, x]);
            }
        }
        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
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
    private TerrainData GetTerrainData() => terrain.terrainData;
    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;
    private Vector3 GetTerrainSize() => GetTerrainData().size;

    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - terrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }
}
