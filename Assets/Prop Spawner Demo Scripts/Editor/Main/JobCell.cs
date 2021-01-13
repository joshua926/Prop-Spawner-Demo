using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace PropSpawner
{
    [BurstCompile(CompileSynchronously = true)]
    public partial struct JobCell : IJob
    {
        [ReadOnly] public int samples;
        [ReadOnly] public TerrainGroup terrainGroup;
        [ReadOnly] public NativeArray<Radii> radii;
        [ReadOnly] public NativeArray<RotSclRange> rotSclRanges;
        [NativeDisableContainerSafetyRestriction] 
        public NativeArray<Cell> cells;
        public Allocator alloc;

        [DeallocateOnJobCompletion]
        public NativeArray<RulesGridData> rulesGridDataArray;
        public Random rand;
        public Shuffle shuffle;
        RulesGridData guestRulesGridData;
        RulesGridData hostRuleSetSlice;
        int guestRuleSetIndex;
        int2 guestCrds;
        float3 worldPos;
        RotScl rotScl;
        int RuleSetCount => radii.Length;

        public void Execute()
        {
            int attemptLimit = 10000000; // for debugging
            int attempts = 0;
            while (shuffle.GetNextCell(ref guestRuleSetIndex, ref guestRulesGridData, out guestCrds, rulesGridDataArray) && 
                attempts < attemptLimit)
            {
                attempts++;
                bool propAdded = false;
                for (int i = 0; i < samples; i++)
                {
                    GenerateProp();
                    if (!terrainGroup.IsOnTerrainTile(worldPos, radii[guestRuleSetIndex].Base)) { continue; }               
                    terrainGroup.SampleHeight(ref worldPos);
                    if (IsValidInGrid())
                    {
                        AddProp();
                        propAdded = true;
                        break;
                    }
                }
                if (!propAdded)
                {
                    shuffle.MarkCurrentCellAsFull(guestRuleSetIndex, guestRulesGridData);
                }
            }
        }       

        void GenerateProp()
        {
            rotScl = rotSclRanges[guestRuleSetIndex].GetRandomRotScl(rand);
            float cellWidth = guestRulesGridData.cellWidth;
            float3 min = new float3(
                terrainGroup.TotalAABB.Min.x + cellWidth * guestCrds.x,
                0,
                terrainGroup.TotalAABB.Min.z + cellWidth * guestCrds.y);
            float3 max = new float3(
                min.x + cellWidth,
                0,
                min.z + cellWidth);
            worldPos = rand.NextFloat3(min, max);
        }

        void AddProp()
        {
            int cellIndex = guestRulesGridData.GetIndex(guestCrds);
            Cell cell = cells[cellIndex];
            if (cell.IsEmpty)
            {
                cell = new Cell(1, alloc);
            }
            cell.positions.Add(worldPos);
            cell.rotScls.Add(rotScl);
            cell.needsSpawning.Add(true);
            cells[cellIndex] = cell;
        }

        bool IsValidInGrid()
        {
            for (int hostRuleSetIndex = 0; hostRuleSetIndex < RuleSetCount; hostRuleSetIndex++)
            {
                hostRuleSetSlice = rulesGridDataArray[hostRuleSetIndex];
                int2 hostCellCount = hostRuleSetSlice.ruleSetCellCounts;
                float hostCellWidth = hostRuleSetSlice.cellWidth;
                int2 guestInHostGridCrds = new int2(
                    (int)((worldPos.x - terrainGroup.TotalAABB.Min.x) / hostCellWidth),
                    (int)((worldPos.z - terrainGroup.TotalAABB.Min.z) / hostCellWidth));
                float likeModelsRadiusSum = radii[hostRuleSetIndex].sameModel * 2;
                float baseRadiusSum = radii[hostRuleSetIndex].Base + radii[guestRuleSetIndex].Base;
                bool sameRuleSet = hostRuleSetIndex == guestRuleSetIndex;
                float checkRadius = sameRuleSet ? likeModelsRadiusSum : baseRadiusSum;
                int ringCount = (int)(checkRadius / hostCellWidth) + 1;
                for (int ring = 0; ring <= ringCount; ring++)
                {
                    int2 minCrds = new int2(
                        math.max(0, guestInHostGridCrds.x - ring),
                        math.max(0, guestInHostGridCrds.y - ring));
                    int2 maxCrds = new int2(
                        math.min(hostCellCount.x - 1, guestInHostGridCrds.x + ring),
                        math.min(hostCellCount.y - 1, guestInHostGridCrds.y + ring));
                    int x = minCrds.x;
                    int y = minCrds.y;
                    while (y <= maxCrds.y)
                    {
                        if (!IsValidInCell(hostRuleSetIndex, new int2(x, y)))
                        {
                            return false;
                        }
                        bool atYEdge = y == (guestInHostGridCrds.y - ring) || y == (guestInHostGridCrds.y + ring);
                        x = atYEdge || x == maxCrds.x ? x + 1 : maxCrds.x;
                        if (x > maxCrds.x)
                        {
                            x = minCrds.x;
                            y++;
                        }
                    }
                }
            }
            return true;
        }

        bool IsValidInCell(int hostRuleSetIndex, int2 hostCrds)
        {
            int hostCellIndex = hostRuleSetSlice.GetIndex(hostCrds);
            Cell cell = cells[hostCellIndex];
            if (cell.IsEmpty) return true;

            for (int i = 0; i < cell.positions.length; i++)
            {
                float sqrDistance = math.distancesq(cell.positions[i], worldPos);
                float sqrSameModelRadiusSum = radii[hostRuleSetIndex].sameModel * 2;
                sqrSameModelRadiusSum *= sqrSameModelRadiusSum;
                float sqrBaseRadiusSum = radii[hostRuleSetIndex].Base + radii[guestRuleSetIndex].Base;
                sqrBaseRadiusSum *= sqrBaseRadiusSum;

                bool sameRuleSet = hostRuleSetIndex == guestRuleSetIndex;
                bool overlappingSameModelRadii = sqrDistance <= sqrSameModelRadiusSum;
                bool overlappingBaseRadii = sqrDistance <= sqrBaseRadiusSum;

                if (overlappingBaseRadii)
                {
                    return false;
                }
                if (sameRuleSet && overlappingSameModelRadii)
                {
                    return false;
                }
            }
            return true;
        }
    }
}