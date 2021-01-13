using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Collections;

namespace PropSpawner
{
    public class SpawnJobManager
    {
        int samples;
        Terrain terrain;
        List<Rules> rulesList;
        GridOfJobs grid;
        PropManager propManager;
        int propCount;
        int progressCheckDelayMs = 250;
        Allocator alloc = Allocator.Persistent;
        public bool Spawning { get; set; }

        public SpawnJobManager(int samples, Terrain terrain, List<Rules> rules)
        {
            this.samples = samples;
            this.terrain = terrain;
            this.rulesList = rules;
        }

        public void BeginSpawningProcess()
        {
            if (!AreValidInputs()) { return; }
            Spawning = true;
            grid = new GridOfJobs(samples, terrain, rulesList, alloc);
            propManager = new PropManager { alloc = alloc };
            propManager.AddLockedPropsToCellsArray(rulesList, grid.rulesGridDataArray, grid.TotalArea, grid.cells);
            grid.ScheduleJobs();

            WaitForJobs();
        }

        bool AreValidInputs()
        {
            bool atLeastOneEnabledRules = false;
            if (rulesList != null)
            {
                foreach (var rule in rulesList)
                {
                    if (rule.IsEnabled)
                    {
                        atLeastOneEnabledRules = true;
                    }
                }
            }
            if (!atLeastOneEnabledRules)
            {
                Debug.LogWarning("Add or enable at least one set of rules to begin spawning.");
                return false;
            }
            if (!terrain)
            {
                Debug.LogWarning("Terrain needed before spawning props");
                return false;
            }
            return true;
        }

        async void WaitForJobs()
        {
            while (!grid.JobsComplete)
            {
                await Task.Delay(progressCheckDelayMs);
            }
            grid.finalJobsHandle.Complete();
            if (Spawning)
            {
                PropManager propManager = new PropManager();
                propManager.DestroyProps(rulesList);
                propManager.InstantiateProps(grid, rulesList);
               // Debug.Log($"{propManager.PropCount} props"); // debugging
            }
            grid.Dispose();
            Spawning = false;
        }       
    }
}