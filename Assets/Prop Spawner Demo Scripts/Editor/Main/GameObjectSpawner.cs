using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace PropSpawner {
    public class GameObjectSpawner
    {
        Rules ruleSet;
        NativeSlice<Cell> cells;
        public int PropCount { get; private set; }

        public GameObjectSpawner(Rules ruleSet, NativeSlice<Cell> cells)
        {
            this.ruleSet = ruleSet;
            this.cells = cells;
        }

        public GameObject[] SpawnGameObjects()
        {
            GameObject prefab = ruleSet.Prefab;

            List<GameObject> props = new List<GameObject>();

            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                Cell cell = cells[cellIndex];
                for (int propIndex = 0, positionsLength = cell.positions.Length; propIndex < positionsLength; propIndex++)
                {
                    PropCount++;
                    if (!cell.needsSpawning[propIndex]) { continue; }
                    GameObject prop = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    prop.name = ruleSet.name;
                    prop.transform.position = cell.positions[propIndex];
                    RotScl rotScl = cell.rotScls[propIndex];
                    prop.transform.rotation = rotScl.rotation;
                    prop.transform.localScale = rotScl.scale;
                    PropSpawnerLink link = prop.AddComponent(typeof(PropSpawnerLink)) as PropSpawnerLink;
                    link.Init(prefab);
                    if (ruleSet.UseECSRendering)
                    {
                        var ecsRend = prop.AddComponent<ECSMeshRenderer>();
                        ecsRend.Init(prefab);
                        // Remove mesh filter, mesh renderer, and LOD group components since ECS Renderer will replace those
                        foreach (var filter in prop.GetComponentsInChildren<MeshFilter>())
                        {
                            GameObject.DestroyImmediate(filter, false);
                        }
                        foreach (var rend in prop.GetComponentsInChildren<MeshRenderer>())
                        {
                            GameObject.DestroyImmediate(rend, false);
                        }
                        foreach (var lodGroup in prop.GetComponentsInChildren<LODGroup>())
                        {
                            GameObject.DestroyImmediate(lodGroup, false);
                        }
                    }
                    props.Add(prop);
                }
            }
            return props.ToArray();
        }
    }
}