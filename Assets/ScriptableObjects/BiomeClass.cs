using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BiomeClass
{
    public string biomeName;
    public Color biomeCol;
    public TileAtlas tileAtlas;
    
    [Header("Noise Settings")]
    public float terrainFreq = .05f;
    public float caveFreq = .05f;
    public Texture2D caveNoiseTexture;

    [Header("Generation Settings")]
    public bool generateCaves = true;
    public int dirtLayerHeight = 5;
    public float surfaceValue = .25f;
    public float heightMultiplier = 4f;
    
    [Header("Trees")]
    public int treeChance = 10;
    public int minTreeHeight = 4;
    public int maxTreeHeight = 6;

    [Header("Addons")] 
    public int tallGrassChance = 10;

    [Header("Ore Settings")] 
    public OreClass[] ores;
}
