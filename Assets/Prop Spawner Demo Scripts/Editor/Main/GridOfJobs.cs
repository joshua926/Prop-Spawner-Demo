using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace PropSpawner
{
    public partial class GridOfJobs
    {
        public NativeArray<Cell> cells;
        public NativeArray<int2> cellsStartLengths;
        public JobHandle finalJobsHandle;
        public NativeArray<RulesGridData> rulesGridDataArray;
        public TerrainGroup TerrainGroup { get; private set; }
        public bool JobsComplete => finalJobsHandle.IsCompleted;
        public AABB TotalArea => TerrainGroup.TotalAABB;

        JobCell[,] jobs;
        JobHandle[,] handles;
        int samples;
        NativeArray<Radii> radii;
        NativeArray<RotSclRange> rotSclRanges;
        Allocator alloc;

        List<Rules> rulesList;
        int ruleSetsCount;
        float jobWidth = 0;
        int2 jobCellCounts;

        public GridOfJobs(
            int samples,
            UnityEngine.Terrain terrain,
            List<Rules> rulesList,
            Allocator alloc)
        {
            this.samples = samples;
            this.rulesList = rulesList;
            this.alloc = alloc;
            ruleSetsCount = rulesList.Count;
            bool isValidTerrainGroup;
            TerrainGroup = new TerrainGroup(terrain, alloc, out isValidTerrainGroup);
            if (!isValidTerrainGroup) { return; }
            PrepJobWidthAndCount();
            PrepArrays();
        }

        void PrepJobWidthAndCount()
        {
            float3 size = TerrainGroup.TotalAABB.Size;
            float maxSameModelRadius = 0;
            for (int i = 0; i < rulesList.Count; i++)
            {
                maxSameModelRadius = math.max(maxSameModelRadius, rulesList[i].Radii.sameModel);
            }
            float area = size.x * size.z;
            int threadCells = UnityEngine.SystemInfo.processorCount * 4;
            float threadArea = area / threadCells;
            float threadWidth = math.sqrt(threadArea);
            jobWidth = math.max(threadWidth, maxSameModelRadius * 2);
            jobCellCounts = new int2(
                (int)math.ceil(size.x / jobWidth),
                (int)math.ceil(size.z / jobWidth));
            jobCellCounts.x = math.max(2, jobCellCounts.x);
            jobCellCounts.y = math.max(2, jobCellCounts.y);
        }

        void PrepArrays()
        {
            radii = new NativeArray<Radii>(ruleSetsCount, alloc);
            rotSclRanges = new NativeArray<RotSclRange>(ruleSetsCount, alloc);
            rulesGridDataArray = new NativeArray<RulesGridData>(ruleSetsCount, alloc);
            cellsStartLengths = new NativeArray<int2>(ruleSetsCount, alloc);
            int totalCells = 0;
            for (int i = 0; i < ruleSetsCount; i++)
            {
                rulesList[i].CalculateCellDetails(jobWidth);
                radii[i] = rulesList[i].Radii;
                rotSclRanges[i] = rulesList[i].RotSclRange;
                int2 cellCounts = new int2(
                    rulesList[i].CellCountPerJob * jobCellCounts.x,
                    rulesList[i].CellCountPerJob * jobCellCounts.y);
                RulesGridData rulesGridData = new RulesGridData
                {
                    ruleSetStartIndexInCellsArray = totalCells,
                    ruleSetCellCounts = cellCounts,
                    cellWidth = rulesList[i].CellWidth,
                    isFull = false,
                    isEnabled = rulesList[i].IsEnabled
                };
                rulesGridDataArray[i] = rulesGridData;
                int length = cellCounts.x * cellCounts.y;
                cellsStartLengths[i] = new int2(totalCells, length);
                totalCells += length;
            }
            cells = new NativeArray<Cell>(totalCells, alloc);
            jobs = new JobCell[jobCellCounts.x, jobCellCounts.y];
            for (int y = 0; y < jobCellCounts.y; y++)
            {
                for (int x = 0; x < jobCellCounts.x; x++)
                {
                    CreateJob(x, y);
                }
            }
        }

        void CreateJob(int x, int y)
        {
            var rulesGridDataArray = new NativeArray<RulesGridData>(this.rulesGridDataArray, alloc);
            for (int i = 0; i < ruleSetsCount; i++)
            {
                RulesGridData ruleSetSlice = rulesGridDataArray[i];
                int cellCountPerJob = rulesList[i].CellCountPerJob;
                ruleSetSlice.minCrdsForJob = new int2(
                    x * cellCountPerJob,
                    y * cellCountPerJob);
                ruleSetSlice.maxCrdsForJob = new int2(
                    math.min(ruleSetSlice.ruleSetCellCounts.x - 1, ruleSetSlice.minCrdsForJob.x + cellCountPerJob - 1),
                    math.min(ruleSetSlice.ruleSetCellCounts.y - 1, ruleSetSlice.minCrdsForJob.y + cellCountPerJob - 1));
                rulesGridDataArray[i] = ruleSetSlice;
            }
            jobs[x, y] = new JobCell
            {
                samples = samples,
                terrainGroup = TerrainGroup,
                radii = radii,
                rotSclRanges = rotSclRanges,
                cells = cells,
                rand = new Random((uint)UnityEngine.Random.Range(1, 1000000)),
                shuffle = new Shuffle(ruleSetsCount, rulesGridDataArray, new Random((uint)UnityEngine.Random.Range(1, 1000000)), alloc),
                rulesGridDataArray = rulesGridDataArray,
                alloc = alloc
            };
        }

        public void ScheduleJobs()
        {
            handles = new JobHandle[jobCellCounts.x, jobCellCounts.y];
            NativeList<JobHandle> depsList = new NativeList<JobHandle>(jobCellCounts.x * jobCellCounts.y / 2, Allocator.Temp);

            for (int yStart = 0; yStart < 2; yStart++)
            {
                for (int xStart = 0; xStart < 2; xStart++)
                {
                    for (int y = yStart; y < jobCellCounts.y; y += 2)
                    {
                        for (int x = xStart; x < jobCellCounts.x; x += 2)
                        {
                            if (xStart == 0 && yStart == 0)
                            {
                                handles[x, y] = jobs[x, y].Schedule();
                            }
                            else if (xStart == 1)
                            {
                                AddHandle(x - 1, y, ref depsList);
                                AddHandle(x + 1, y, ref depsList);
                                JobHandle deps = JobHandle.CombineDependencies(depsList);
                                handles[x, y] = jobs[x, y].Schedule(deps);
                            }
                            else
                            {
                                AddHandle(x - 1, y - 1, ref depsList);
                                AddHandle(x + 1, y - 1, ref depsList);
                                AddHandle(x - 1, y + 1, ref depsList);
                                AddHandle(x + 1, y + 1, ref depsList);
                                JobHandle deps = JobHandle.CombineDependencies(depsList);
                                handles[x, y] = jobs[x, y].Schedule(deps);
                            }
                            depsList.Clear();
                        }
                    }
                }
            }

            // combine quadrant 3 jobs into final job handle
            for (int y = 1; y < jobCellCounts.y; y += 2)
            {
                for (int x = 1; x < jobCellCounts.x; x += 2)
                {
                    AddHandle(x, y, ref depsList);
                }
            }
            finalJobsHandle = JobHandle.CombineDependencies(depsList);
            depsList.Dispose();

            void AddHandle(int xCrd, int yCrd, ref NativeList<JobHandle> list)
            {
                if (
                    xCrd >= 0 &&
                    yCrd >= 0 &&
                    xCrd < jobCellCounts.x &&
                    yCrd < jobCellCounts.y)
                {
                    list.Add(handles[xCrd, yCrd]);
                }
            }
        }

        public float GetProgress()
        {
            int completed = 0;
            int handlesCount = jobCellCounts.x * jobCellCounts.y;
            for (int i = 0; i < handlesCount; i++)
            {
                int x = i % jobCellCounts.x;
                int y = i / jobCellCounts.x;
                if (handles[x, y].IsCompleted) { completed++; }
            }
            return (float)completed / handlesCount;
        }

        public void Dispose()
        {
            TerrainGroup.Dispose();
            radii.Dispose();
            rotSclRanges.Dispose();
            rulesGridDataArray.Dispose();
            cellsStartLengths.Dispose();
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Dispose();
            }
            cells.Dispose();
            foreach (var job in jobs)
            {
                job.shuffle.Dispose();
            }
        }

        ~GridOfJobs() // debugging
        {
            CompleteJobs();
        }

        void CompleteJobs()
        {
            if (!JobsComplete)
            {
                finalJobsHandle.Complete();
                Dispose();
            }
        }
    }
}