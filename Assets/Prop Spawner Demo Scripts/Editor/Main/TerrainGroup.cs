using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace PropSpawner
{
    public struct TerrainGroup
    {
        int2 heightMapResolution;
        NativeArray<float> heightMap;
        float3 terrainSize;
        int2 terrainCounts;
        NativeArray<bool> fullTiles;
        public AABB TotalAABB { get; private set; }

        /// <summary>
        /// Returns false if terrain group is invalid.
        /// </summary>        
        public TerrainGroup(Terrain inputTerrain, Allocator alloc, out bool isValidTerrain)
        {
            isValidTerrain = true;            

            // Add all connected terrains to dictionary
            Dictionary<Terrain, int2> terrainCrdsDict = new Dictionary<Terrain, int2>();
            int2 min = new int2();
            int2 max = new int2();
            FindNeighbors(inputTerrain, int2.zero);
            void FindNeighbors(Terrain terrain, int2 crds)
            {
                if (!terrain || terrainCrdsDict.ContainsKey(terrain)) return;
                terrainCrdsDict.Add(terrain, crds);
                min = math.min(min, crds);
                max = math.max(max, crds);
                FindNeighbors(terrain.topNeighbor, crds + new int2(0, 1));
                FindNeighbors(terrain.bottomNeighbor, crds + new int2(0, -1));
                FindNeighbors(terrain.leftNeighbor, crds + new int2(-1, 0));
                FindNeighbors(terrain.rightNeighbor, crds + new int2(1, 0));
            }

            // Fill 2d terrains array, get total AABB of all connected terrains, and fill fullTiles array
            terrainCounts = max - min + new int2(1, 1);
            Terrain[,] terrains = new Terrain[terrainCounts.x, terrainCounts.y];
            Bounds totalBounds = GetWorldBounds(inputTerrain);
            foreach (var terrain in terrainCrdsDict.Keys)
            {
                int2 crds = terrainCrdsDict[terrain] - min;
                terrains[crds.x, crds.y] = terrain;
                totalBounds.Encapsulate(GetWorldBounds(terrain));
            }
            Bounds GetWorldBounds(Terrain terrain)
            {
                Vector3 terrainSize = terrain.terrainData.size;
                return new Bounds(terrain.terrainData.bounds.min + terrain.transform.position + terrainSize / 2, terrainSize);
            }
            TotalAABB = new AABB() { Center = totalBounds.center, Extents = totalBounds.extents };
            fullTiles = new NativeArray<bool>(terrainCounts.x * terrainCounts.y, alloc);
            for (int i = 0; i < fullTiles.Length; i++)
            {
                int x = i % terrainCounts.x;
                int y = i / terrainCounts.x;
                fullTiles[i] = terrains[x, y];
            }

            // Make sure all connected terrain tiles have equal size and heightmap resolution 
            int resolution = inputTerrain.terrainData.heightmapResolution;
            heightMapResolution = terrainCounts * resolution;
            heightMap = new NativeArray<float>(heightMapResolution.x * heightMapResolution.y, alloc);
            terrainSize = inputTerrain.terrainData.size;
            for (int i = 0; i < terrainCounts.x * terrainCounts.y; i++)
            {
                int x = i % terrainCounts.x;
                int y = i / terrainCounts.x;
                Terrain terrain = terrains[x, y];
                if (terrain &&
                    (terrain.terrainData.heightmapResolution != resolution ||
                    terrain.terrainData.size != (Vector3)terrainSize))
                {
                    Debug.LogWarning("All connected terrain tiles must have the same size and " +
                        "heightmap resolution before props can be spawned.");
                    isValidTerrain = false;
                    break;
                }
            }

            // Fill heightMap       
            for (int ty = 0; ty < terrainCounts.y; ty++)
            {
                for (int tx = 0; tx < terrainCounts.x; tx++)
                {
                    Terrain terrain = terrains[tx, ty];
                    if (!terrain) { continue; }
                    float[,] values = terrain.terrainData.GetHeights(0, 0, resolution, resolution);
                    for (int y = 0; y < resolution; y++)
                    {
                        for (int x = 0; x < resolution; x++)
                        {
                            int i = x + tx * resolution + (y + ty * resolution) * heightMapResolution.x;
                            heightMap[i] = values[y, x];
                        }
                    }
                }
            }
        }

        public void SampleHeight(ref float3 worldPos)
        {
            float3 localPos = worldPos - TotalAABB.Min;
            localPos = math.min(localPos, TotalAABB.Size - new float3(.001f, .001f, .001f));
            float2 sampleValue = new float2(
                localPos.x / TotalAABB.Size.x,
                localPos.z / TotalAABB.Size.z);
            float2 samplePos = new float2(
                 sampleValue.x * (heightMapResolution.x - 1),
                 sampleValue.y * (heightMapResolution.y - 1));
            int2 sampleFloor = new int2(
                (int)samplePos.x,
                (int)samplePos.y);
            float2 sampleDecimal = new float2(
                samplePos.x - sampleFloor.x,
                samplePos.y - sampleFloor.y);
            int upperLeftTri = sampleDecimal.y > sampleDecimal.x ? 1 : 0;

            float3 v0 = GetVertexLocalPos(sampleFloor.x, sampleFloor.y);
            float3 v1 = GetVertexLocalPos(sampleFloor.x + 1, sampleFloor.y + 1);
            int upperLeftOrLowerRightX = sampleFloor.x + 1 - upperLeftTri;
            int upperLeftOrLowerRightY = sampleFloor.y + upperLeftTri;
            float3 v2 = GetVertexLocalPos(upperLeftOrLowerRightX, upperLeftOrLowerRightY);
            float3 n = math.cross(v1 - v0, v2 - v0);            
            // based on plane formula: a(x - x0) + b(y - y0) + c(z - z0) = 0
            float localY = ((-n.x * (localPos.x - v0.x) - n.z * (localPos.z - v0.z)) / n.y) + v0.y;
            worldPos.y = localY + TotalAABB.Min.y;
        }

        public bool IsOnTerrainTile(float3 worldPos, float radius)
        {
            if (worldPos.x - radius <= TotalAABB.Min.x ||
                worldPos.z - radius <= TotalAABB.Min.z ||
                worldPos.x + radius > TotalAABB.Max.x ||
                worldPos.z + radius > TotalAABB.Max.z)
            {
                return false;
            }

            float3 localPos = worldPos - TotalAABB.Min;
            int2 terrainCrds = new int2(
                (int)(localPos.x / terrainSize.x),
                (int)(localPos.z / terrainSize.z));
            int i = terrainCrds.x + terrainCrds.y * terrainCounts.x;
            return fullTiles[i];
        }

        float3 GetVertexLocalPos(int x, int y)
        {
            int index = x + y * heightMapResolution.x;
            float heightValue = heightMap[index];
            return new float3(
                (float)x / (heightMapResolution.x - 1) * TotalAABB.Size.x,
                heightValue * TotalAABB.Size.y,
                (float)y / (heightMapResolution.y - 1) * TotalAABB.Size.z);
        }

        public void Dispose()
        {
            heightMap.Dispose();
            fullTiles.Dispose();
        }
    }
}