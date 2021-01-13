using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace PropSpawner
{
    public class PropManager
    {
        public Allocator alloc;
        public int PropCount { get; private set; }

        public void DestroyProps(List<Rules> rulesList)
        {
            var prefabs = PropSpawnerLink.GetPrefabs();
            for (int i = prefabs.Count - 1; i >= 0; i--)
            {
                Rules rules = rulesList.Find(x => x.Prefab == prefabs[i]);
                if (!rules || !rules.IsEnabled) { continue; }
                var links = PropSpawnerLink.GetLinks(prefabs[i]);
                for (int j = links.Count - 1; j >= 0; j--)
                {
                    var link = links[j];
                    if (link == null || link.isLocked) { continue; }
                    GameObject.DestroyImmediate(link.gameObject);
                }
            }
        }

        public void InstantiateProps(GridOfJobs grid, List<Rules> rulesList)
        {
            PropCount = 0;
            NativeArray<Cell> cells = grid.cells;
            for (int ruleSetIndex = 0; ruleSetIndex < rulesList.Count; ruleSetIndex++)
            {
                Rules rules = rulesList[ruleSetIndex];
                if (!rules.IsEnabled) { continue; }
                GameObject parent = GroupManager.GetGroupGameObject(rules);
                int start = grid.cellsStartLengths[ruleSetIndex].x;
                int length = grid.cellsStartLengths[ruleSetIndex].y;

                InstancerSpawner instancerSpawner;
                GameObjectSpawner goSpawner;
                if (rules.KeepGameObjects)
                {
                    goSpawner = new GameObjectSpawner(rules, cells.Slice(start, length));
                    foreach (var go in goSpawner.SpawnGameObjects())
                    {
                        if (!go) { continue; }
                        go.transform.SetParent(parent.transform);
                    }
                    PropCount += goSpawner.PropCount;
                }
                else if (rules.UseECSRendering)
                {
                    instancerSpawner = new InstancerSpawner(
                        rules,
                        cells.Slice(start, length),
                        grid.TotalArea,
                        grid.TerrainGroup);
                    foreach (var instancer in instancerSpawner.SpawnInstancers())
                    {
                        if (!instancer) { continue; }
                        instancer.transform.SetParent(parent.transform);
                    }
                    PropCount += instancerSpawner.PropCount;
                }
            }
        }

        public void AddLockedPropsToCellsArray(
            List<Rules> rulesList,
            NativeArray<RulesGridData> rulesGridDataArray,
            AABB totalArea,
            NativeArray<Cell> cells)
        {
            RulesGridData rulesGridData;
            var prefabs = PropSpawnerLink.GetPrefabs();
            foreach (var prefab in prefabs)
            {
                Rules rules = rulesList.Find(x => x.Prefab == prefab);
                if (rules == null) { continue; }
                int rulesIndex = rulesList.IndexOf(rules);
                rulesGridData = rulesGridDataArray[rulesIndex];

                var linkList = PropSpawnerLink.GetLinks(prefab);
                for (int i = 0, length = linkList.Count; i < length; i++)
                {
                    var link = linkList[i];
                    if (link == null) { continue; }
                    if (link.isLocked || !rules.IsEnabled)
                    {
                        var ecsInstancer = link.GetComponent<ECSMeshInstancer>();
                        // If there is an ecsInstancer component then add all its instances as preexisting props
                        if (ecsInstancer)
                        {
                            for (int p = 0, instanceCount = ecsInstancer.InstanceCount; p < instanceCount; p++)
                            {
                                var tran = ecsInstancer[p];
                                AddLockedProp(tran.position, tran.rotation, tran.scale);
                            }
                        }
                        else
                        {
                            AddLockedProp(link.transform.position, link.transform.rotation, link.transform.localScale);
                        }
                    }
                }
            }
            void AddLockedProp(float3 pos, quaternion rot, float3 scl)
            {
                int2 guestCrds = new int2(
                    (int)((pos.x - totalArea.Min.x) / rulesGridData.cellWidth),
                    (int)((pos.z - totalArea.Min.z) / rulesGridData.cellWidth));
                int cellIndex = rulesGridData.GetIndex(guestCrds);
                Cell cell = cells[cellIndex];
                if (cell.IsEmpty)
                {
                    cell = new Cell(1, alloc);
                }
                cell.positions.Add(pos);
                cell.rotScls.Add(new RotScl { rotation = rot, scale = scl });
                cell.needsSpawning.Add(false);
                cells[cellIndex] = cell;
            }
        }
    }
}