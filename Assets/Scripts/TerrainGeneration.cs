using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// 地形生成类，用于在 Unity 中生成 2D 噪声地形。
/// 使用 Perlin 噪声算法生成地形高度和洞穴结构，并根据噪声纹理决定是否生成地块。
/// </summary>
public class TerrainGeneration : MonoBehaviour
{
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;
    
    [Header("Trees")]
    public int treeChance = 10;
    public int minTreeHeight = 4;
    public int maxTreeHeight = 6;

    [Header("Addons")] 
    public int tallGrassChance = 10;

    [Header("Generation Settings")]
    public int chunkSize = 16;
    public int worldSize = 100;
    public bool generateCaves = true;
    public int dirtLayerHeight = 5;
    public float surfaceValue = .25f;
    public float heightMultiplier = 4f;
    public int heightAddition = 25;
    
    [Header("Noise Settings")]
    public float terrainFreq = .05f;
    public float caveFreq = .05f;
    public float seed;
    public Texture2D caveNoiseTexture;

    [Header("Ore Settings")] 
    public OreClass[] ores;

    private GameObject[] worldChunks;
    private List<Vector2> worldTiles = new List<Vector2>();

    private void OnValidate()
    {
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        ores[0].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[1].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[2].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[3].spreadTexture = new Texture2D(worldSize, worldSize);
        
        
        GenerateNoiseTexture(caveFreq,  surfaceValue, caveNoiseTexture);
        
        GenerateNoiseTexture(ores[0].rarity,  ores[0].size, ores[0].spreadTexture);
        GenerateNoiseTexture(ores[1].rarity,  ores[1].size, ores[1].spreadTexture);
        GenerateNoiseTexture(ores[2].rarity,  ores[2].size, ores[2].spreadTexture);
        GenerateNoiseTexture(ores[3].rarity,  ores[3].size, ores[3].spreadTexture);
    }

    /// <summary>
    /// 初始化地形生成逻辑，在游戏对象启动时调用。
    /// 随机生成一个种子值，并依次生成噪声纹理和地形。
    /// </summary>
    private void Start()
    {
        seed = Random.Range(-10000, 10000);
    
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        ores[0].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[1].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[2].spreadTexture = new Texture2D(worldSize, worldSize);
        ores[3].spreadTexture = new Texture2D(worldSize, worldSize);
        
        
        GenerateNoiseTexture(caveFreq,  surfaceValue, caveNoiseTexture);
        
        GenerateNoiseTexture(ores[0].rarity,  ores[0].size, ores[0].spreadTexture);
        GenerateNoiseTexture(ores[1].rarity,  ores[1].size, ores[1].spreadTexture);
        GenerateNoiseTexture(ores[2].rarity,  ores[2].size, ores[2].spreadTexture);
        GenerateNoiseTexture(ores[3].rarity,  ores[3].size, ores[3].spreadTexture);
        
        CreateChunks();
        GenerateTerrain();
    }

    private void CreateChunks()
    {
        int numChucks = worldSize / chunkSize;
        worldChunks = new GameObject[numChucks];
        for (int i = 0; i < numChucks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    /// <summary>
    /// 根据噪声纹理和地形高度生成实际的地形块。
    /// 每一列根据 Perlin 噪声计算高度，然后在该高度范围内检查噪声纹理，
    /// 如果纹理中某点的红色通道值大于 surfaceValue，则在该位置生成一个地块。
    /// </summary>
    private void GenerateTerrain()
    {
        // 遍历每一列（x 坐标）
        for (int x = 0; x < worldSize; x++)
        {
            // 使用 Perlin 噪声计算当前列的地形高度
            float height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * heightMultiplier + heightAddition;

            // 遍历该列中从底部到计算高度的所有行（y 坐标）
            for (int y = 0; y < height; y++)
            {
                Sprite[] tileSprites;
                
                if (y < height - dirtLayerHeight)
                {
                    tileSprites = tileAtlas.stone.tileSprites;
                    
                    if (ores[0].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[0].maxSpawnHeight)
                        tileSprites = tileAtlas.coal.tileSprites;
                    
                    if (ores[1].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[1].maxSpawnHeight)
                        tileSprites = tileAtlas.iron.tileSprites;
                    
                    if (ores[2].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[2].maxSpawnHeight)
                        tileSprites = tileAtlas.gold.tileSprites;
                    
                    if (ores[3].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[3].maxSpawnHeight)
                        tileSprites = tileAtlas.diamond.tileSprites;
                }
                else if (y < height - 1)
                {
                    tileSprites = tileAtlas.dirt.tileSprites;
                }
                else
                {
                    tileSprites = tileAtlas.grass.tileSprites;
                }

                if (generateCaves)
                {
                    // 如果噪声纹理中该点的红色通道值大于阈值，则生成地块
                    if (caveNoiseTexture.GetPixel(x, y).r > .5f)
                    {
                        PlaceTile(tileSprites, x, y);
                    }
                }
                else
                {
                    PlaceTile(tileSprites, x, y);
                }

                if (y >= height - 1)
                {
                    int t = Random.Range(0, treeChance);
                    if (t == 1)
                    {
                        if (worldTiles.Contains(new Vector2(x, y)))
                            GenerateTree(x, y + 1);
                    }
                    else
                    {
                        int i = Random.Range(0, tallGrassChance);
                        if (i == 1)
                        {
                            if (worldTiles.Contains(new Vector2(x, y)))
                                PlaceTile(tileAtlas.tallGrass.tileSprites, x, y + 1);   
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// 生成用于决定地形中哪些位置应生成地块的噪声纹理。
    /// 使用 Perlin 噪声填充纹理的每个像素，颜色值表示该点的噪声强度。
    /// </summary>
    private void GenerateNoiseTexture(float frequency, float limit, Texture2D noiseTexture)
    {
        // 遍历纹理的每个像素
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                // 使用 Perlin 噪声生成像素值
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                if (v > limit)
                // 将噪声值设置为 RGB 三个通道，形成灰度图
                    noiseTexture.SetPixel(x, y, Color.white);
                else
                    noiseTexture.SetPixel(x, y, Color.black);
            }
        }
        
        // 应用纹理更改
        noiseTexture.Apply();
    }

    /// <summary>
    /// 在指定坐标生成一棵树。
    /// 树由树干和树叶组成，树干使用 log 精灵，树叶使用 leaf 精灵。
    /// </summary>
    /// <param name="x">树木底部中心的 x 坐标</param>
    /// <param name="y">树木底部中心的 y 坐标</param>
    void GenerateTree(int x, int y)
    {
        int treeHeight = Random.Range(minTreeHeight, maxTreeHeight);
        for (int i = 0; i < treeHeight; i++)
        {
            PlaceTile(tileAtlas.log.tileSprites, x, y + i);    
        }
        
        for (int i = 0; i < 3; i++) 
        {
            PlaceTile(tileAtlas.leaf.tileSprites, x, y + treeHeight + i);
        }
    
        for (int i = 0; i < 2; i++)
        {
            PlaceTile(tileAtlas.leaf.tileSprites, x - 1, y + treeHeight + i);
            PlaceTile(tileAtlas.leaf.tileSprites, x + 1, y + treeHeight + i);
        }
    }
    
    /// <summary>
    /// 在指定坐标创建一个新的地块对象。
    /// 新对象将作为当前对象的子对象，并设置其精灵和位置。
    /// </summary>
    /// <param name="tileSprite">要使用的精灵</param>
    /// <param name="x">地块的 x 坐标</param>
    /// <param name="y">地块的 y 坐标</param>
    private void PlaceTile(Sprite[] tileSprites, int x, int y)
    {
        GameObject newTile = new GameObject();

        float chunkCoord = Mathf.Round(x / chunkSize) * chunkSize;
        chunkCoord /= chunkSize;
        newTile.transform.parent = worldChunks[(int)chunkCoord].transform;
        
        newTile.AddComponent<SpriteRenderer>();
        int spriteIndex = Random.Range(0, tileSprites.Length);
        newTile.GetComponent<SpriteRenderer>().sprite = tileSprites[spriteIndex];
        newTile.name = tileSprites[0].name;
        newTile.transform.position = new Vector2(x + .5f, y + .5f);
        
        worldTiles.Add(newTile.transform.position - Vector3.one * .5f);
    }
}
