using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PropSpawner
{
    public struct Cell
    {
        public UnsafeList<float3> positions;
        public UnsafeList<RotScl> rotScls;
        public UnsafeList<bool> needsSpawning;
        public bool IsEmpty { get { return !positions.IsCreated; } }

        public Cell(int capacity, Allocator alloc)
        {
            positions = new UnsafeList<float3>(capacity, alloc, NativeArrayOptions.ClearMemory);
            rotScls = new UnsafeList<RotScl>(capacity, alloc, NativeArrayOptions.ClearMemory);
            needsSpawning = new UnsafeList<bool>(capacity, alloc, NativeArrayOptions.ClearMemory);
        }

        public void Dispose()
        {
            positions.Dispose();
            rotScls.Dispose();
            needsSpawning.Dispose();
        }
    }

    public struct RotSclRange
    {
        public float3 rMin;
        public float3 rMax;
        public float3 sMin;
        public float3 sMax;

        public RotScl GetRandomRotScl(Unity.Mathematics.Random rand)
        {
            float3 randRot = rand.NextFloat3(rMin, rMax);
            quaternion rot = quaternion.Euler(randRot);
            float3 scl = rand.NextFloat3(sMin, sMax);
            return new RotScl { rotation = rot, scale = scl };        
        }
    }

    public struct RotScl
    {
        public quaternion rotation;
        public float3 scale;
    }

    public struct Radii
    {
        public float Base;
        public float sameModel;
    }

    public struct RulesGridData
    {
        public int ruleSetStartIndexInCellsArray;
        public int2 ruleSetCellCounts;
        public float cellWidth;
        public int2 minCrdsForJob;
        public int2 maxCrdsForJob;
        public int currentShuffleIndex;
        public bool isFull;
        public bool isEnabled;
        public int Length { get { return ((maxCrdsForJob - minCrdsForJob).x + 1) * ((maxCrdsForJob - minCrdsForJob).y + 1); } }
        public int GetIndex(int2 crds)
        {
            return ruleSetStartIndexInCellsArray + crds.x + crds.y * ruleSetCellCounts.x;
        }
    }
}
