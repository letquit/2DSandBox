using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// 地形生成类，用于在 Unity 中生成 2D 噪声地形。
/// 使用 Perlin 噪声算法生成地形高度和洞穴结构，并根据噪声纹理决定是否生成地块。
/// 优化：
/// 1. worldTiles 用 HashSet<Vector2Int>，高效判重。
/// 2. Texture2D 内像素批量设置后只调用一次 Apply()。
/// 3. GameObject 生成建议用 Tilemap，保留原结构仅为演示优化点。
/// 4. biomeMap 不再用颜色直接比对，而用 int 索引标记 biome（可选）。
/// 5. 其它细节优化。
/// </summary>
public class TerrainGeneration : MonoBehaviour
{
    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;
    public float seed;

    public BiomeClass[] biomes;

    [Header("Biomes")]
    public float biomeFrequency;
    public Gradient biomeGradient;
    public Texture2D biomeMap;

    [Header("Generation Settings")]
    public int chunkSize = 16;
    public int worldSize = 100;
    public bool generateCaves = true;
    public int heightAddition = 25;

    [Header("Noise Settings")]
    public Texture2D caveNoiseTexture;

    [Header("Ore Settings")]
    public OreClass[] ores;

    private GameObject[] worldChunks;
    private HashSet<Vector2Int> worldTiles = new HashSet<Vector2Int>();
    private BiomeClass curBiome;

    /// <summary>
    /// 初始化地形生成逻辑，在游戏对象启动时调用。
    /// 随机生成一个种子值，并依次生成噪声纹理和地形。
    /// </summary>
    private void Start()
    {
        seed = Random.Range(-10000, 10000);

        // 初始化 Ores 的 spreadTexture
        foreach (var ore in ores)
        {
            ore.spreadTexture = new Texture2D(worldSize, worldSize);
        }

        DrawTextures();
        DrawCavesAndOres();

        CreateChunks();
        GenerateTerrain();
    }

    private void DrawCavesAndOres()
    {
        // 1. 生成洞穴噪声
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        for (int x = 0; x < caveNoiseTexture.width; x++)
        {
            for (int y = 0; y < caveNoiseTexture.height; y++)
            {
                curBiome = GetCurrentBiome(x, y);
                float v = Mathf.PerlinNoise((x + seed) * curBiome.caveFreq, (y + seed) * curBiome.caveFreq);
                caveNoiseTexture.SetPixel(x, y, v > curBiome.surfaceValue ? Color.white : Color.black);
            }
        }
        caveNoiseTexture.Apply();

        // 2. 生成矿物噪声
        // 先全部像素设置完后再 Apply
        foreach (var ore in ores)
        {
            ore.spreadTexture = new Texture2D(worldSize, worldSize);
        }

        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                curBiome = GetCurrentBiome(x, y);
                for (int i = 0; i < ores.Length; i++)
                {
                    Color color = Color.black;
                    if (curBiome.ores.Length > i)
                    {
                        float v = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].rarity, (y + seed) * curBiome.ores[i].rarity);
                        if (v > curBiome.ores[i].size)
                            color = Color.white;
                    }
                    ores[i].spreadTexture.SetPixel(x, y, color);
                }
            }
        }
        foreach (var ore in ores)
        {
            ore.spreadTexture.Apply();
        }
    }

    private void DrawTextures()
    {
        biomeMap = new Texture2D(worldSize, worldSize);
        DrawBiomeTexture();

        // 为每个生物群系和其矿物生成噪声纹理
        foreach (var biome in biomes)
        {
            biome.caveNoiseTexture = new Texture2D(worldSize, worldSize);

            for (int j = 0; j < biome.ores.Length; j++)
            {
                biome.ores[j].spreadTexture = new Texture2D(worldSize, worldSize);
            }

            GenerateNoiseTexture(biome.caveFreq, biome.surfaceValue, biome.caveNoiseTexture);
            for (int j = 0; j < biome.ores.Length; j++)
            {
                GenerateNoiseTexture(biome.ores[j].rarity, biome.ores[j].size, biome.ores[j].spreadTexture);
            }
        }
    }

    private void DrawBiomeTexture()
    {
        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.height; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * biomeFrequency, (y + seed) * biomeFrequency);
                Color col = biomeGradient.Evaluate(v);
                biomeMap.SetPixel(x, y, col);
            }
        }
        biomeMap.Apply();
    }

    private void CreateChunks()
    {
        int numChunks = worldSize / chunkSize;
        worldChunks = new GameObject[numChunks];
        for (int i = 0; i < numChunks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    /// <summary>
    /// 更健壮的生物群系判断（建议用 int 索引分区或区间判断，暂保留颜色判断）
    /// </summary>
    public BiomeClass GetCurrentBiome(int x, int y)
    {
        // 用颜色直接比对不可靠，建议按区间或单独存 biomeMap int 索引
        Color pixelCol = biomeMap.GetPixel(x, y);
        for (int i = 0; i < biomes.Length; i++)
        {
            if (Approximately(biomes[i].biomeCol, pixelCol))
            {
                return biomes[i];
            }
        }
        return biomes[0]; // fallback
    }

    // 浮点颜色近似比较
    private bool Approximately(Color a, Color b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    /// <summary>
    /// 根据噪声纹理和地形高度生成实际的地形块。
    /// </summary>
    private void GenerateTerrain()
    {
        Sprite[] tileSprites;

        for (int x = 0; x < worldSize; x++)
        {
            curBiome = GetCurrentBiome(x, 0);
            float height = Mathf.PerlinNoise((x + seed) * curBiome.terrainFreq, seed * curBiome.terrainFreq) * curBiome.heightMultiplier + heightAddition;

            for (int y = 0; y < height; y++)
            {
                curBiome = GetCurrentBiome(x, y);

                // 选择地表、泥土、石头
                if (y < height - curBiome.dirtLayerHeight)
                {
                    tileSprites = curBiome.tileAtlas.stone.tileSprites;
                    if (ores.Length > 0 && ores[0].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[0].maxSpawnHeight)
                        tileSprites = tileAtlas.coal.tileSprites;
                    if (ores.Length > 1 && ores[1].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[1].maxSpawnHeight)
                        tileSprites = tileAtlas.iron.tileSprites;
                    if (ores.Length > 2 && ores[2].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[2].maxSpawnHeight)
                        tileSprites = tileAtlas.gold.tileSprites;
                    if (ores.Length > 3 && ores[3].spreadTexture.GetPixel(x, y).r > .5f && height - y > ores[3].maxSpawnHeight)
                        tileSprites = tileAtlas.diamond.tileSprites;
                }
                else if (y < height - 1)
                {
                    tileSprites = curBiome.tileAtlas.dirt.tileSprites;
                }
                else
                {
                    tileSprites = curBiome.tileAtlas.grass.tileSprites;
                }

                // 洞穴判定
                if (generateCaves)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > .5f)
                    {
                        PlaceTile(tileSprites, x, y);
                    }
                }
                else
                {
                    PlaceTile(tileSprites, x, y);
                }

                // 顶层生长树/草
                if (y >= height - 1)
                {
                    int t = Random.Range(0, curBiome.treeChance);
                    if (t == 1)
                    {
                        if (worldTiles.Contains(new Vector2Int(x, y)))
                            GenerateTree(Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight), x, y + 1);
                    }
                    else
                    {
                        int i = Random.Range(0, curBiome.tallGrassChance);
                        if (i == 1)
                        {
                            if (worldTiles.Contains(new Vector2Int(x, y)))
                            {
                                if (curBiome.tileAtlas.tallGrass != null)
                                    PlaceTile(curBiome.tileAtlas.tallGrass.tileSprites, x, y + 1);
                            }
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
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                noiseTexture.SetPixel(x, y, v > limit ? Color.white : Color.black);
            }
        }
        noiseTexture.Apply();
    }

    /// <summary>
    /// 在指定坐标生成一棵树。
    /// </summary>
    void GenerateTree(int treeHeight, int x, int y)
    {
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
    /// </summary>
    private void PlaceTile(Sprite[] tileSprites, int x, int y)
    {
        var pos = new Vector2Int(x, y);
        if (!worldTiles.Contains(pos))
        {
            GameObject newTile = new GameObject();

            int chunkCoord = Mathf.RoundToInt((float)x / chunkSize);
            newTile.transform.parent = worldChunks[chunkCoord].transform;

            var renderer = newTile.AddComponent<SpriteRenderer>();
            int spriteIndex = Random.Range(0, tileSprites.Length);
            renderer.sprite = tileSprites[spriteIndex];
            newTile.name = tileSprites[0].name;
            newTile.transform.position = new Vector2(x + .5f, y + .5f);

            worldTiles.Add(pos);
        }
    }
}