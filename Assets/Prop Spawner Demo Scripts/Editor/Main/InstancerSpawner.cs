using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEditor;

namespace PropSpawner
{
    public class InstancerSpawner
    {
        Rules ruleSet;
        NativeSlice<Cell> cells;
        AABB area;
        TerrainGroup terrainGroup;
        public int PropCount { get; private set; }

        public InstancerSpawner(Rules ruleSet, NativeSlice<Cell> cells, AABB area, TerrainGroup terrainGroup)
        {
            this.ruleSet = ruleSet;
            this.cells = cells;
            this.area = area;
            this.terrainGroup = terrainGroup;
        }

        public GameObject[] SpawnInstancers()
        {
            GameObject prefab = ruleSet.Prefab;            
            float2 zoneSize = ruleSet.ZoneSize;
            int2 zoneCounts = new int2(
                (int)math.ceil(area.Size.x / zoneSize.x),
                (int)math.ceil(area.Size.z / zoneSize.y));
            GameObject[] instancers = new GameObject[zoneCounts.x * zoneCounts.y];
            Zone[] zones = new Zone[instancers.Length];

            // Create one ECS Mesh Instancer per zone and give each one all transforms of props in its zone
            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                Cell cell = cells[cellIndex];
                for (int propIndex = 0, positionsLength = cell.positions.Length; propIndex < positionsLength; propIndex++)
                {
                    PropCount++;
                    float3 t = cell.positions[propIndex];
                    RotScl rotScl = cell.rotScls[propIndex];
                    quaternion r = rotScl.rotation;
                    float3 s = rotScl.scale;
                    int2 zoneCrds = new int2(
                        (int)((t.x - area.Min.x) / zoneSize.x),
                        (int)((t.z - area.Min.z) / zoneSize.y));
                    int zoneIndex = zoneCrds.x + zoneCrds.y * zoneCounts.x;
                    if (!instancers[zoneIndex])
                    {
                        var instancer = new GameObject($"{ruleSet.name} - Instancer ({zoneCrds.x},{zoneCrds.y})");
                        float3 position = new Vector3(
                            (zoneCrds.x + .5f) * zoneSize.x,
                            0,
                            (zoneCrds.y + .5f) * zoneSize.y);
                        terrainGroup.SampleHeight(ref position);
                        instancer.transform.position = position;
                        var link = instancer.AddComponent<PropSpawnerLink>();
                        link.Init(prefab);
                        instancers[zoneIndex] = instancer;
                    }
                    if (zones[zoneIndex] == null)
                    {
                        zones[zoneIndex] = new Zone();
                    }
                    var zone = zones[zoneIndex];
                    zone.translations.Add(t);
                    zone.rotations.Add(r);
                    zone.scales.Add(s);
                }
            }
            for (int i = 0; i < zones.Length; i++)
            {
                var instancer = instancers[i];
                if (!instancer) { continue; } // debugging
                ECSMeshInstancer instancerComponent = instancer.AddComponent<ECSMeshInstancer>();
                instancerComponent.Init(
                    prefab,
                    zones[i].translations.ToArray(),
                    zones[i].rotations.ToArray(),
                    zones[i].scales.ToArray());
            }
            return instancers;
        }

        class Zone
        {
            public List<float3> translations = new List<float3>();
            public List<quaternion> rotations = new List<quaternion>();
            public List<float3> scales = new List<float3>();
        }
    }
}