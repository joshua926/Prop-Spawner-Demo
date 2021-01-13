using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PropSpawner
{
    public struct Shuffle
    {
        public NativeArray<UnsafeList<CellData>> arrayOfShuffles;
        int ruleSetCount;

        public struct CellData
        {
            public int2 crds;
            public bool isFull;
        }

        public Shuffle(int ruleSetCount, NativeArray<RulesGridData> rulesGridDataArray, Random rand, Allocator alloc)
        {
            this.ruleSetCount = ruleSetCount;
            arrayOfShuffles = new NativeArray<UnsafeList<CellData>>(ruleSetCount, alloc);           
            for (int ruleSetIndex = 0; ruleSetIndex < ruleSetCount; ruleSetIndex++)
            {
                RulesGridData rulesGridData = rulesGridDataArray[ruleSetIndex];
                UnsafeList<CellData> shuffle = new UnsafeList<CellData>(rulesGridData.Length, alloc);
                // If this rules is not enabled then add an empty list of cells to the shuffle at its index
                if (rulesGridData.isEnabled)
                {
                    for (int y = rulesGridData.minCrdsForJob.y; y <= rulesGridData.maxCrdsForJob.y; y++)
                    {
                        for (int x = rulesGridData.minCrdsForJob.x; x <= rulesGridData.maxCrdsForJob.x; x++)
                        {
                            shuffle.Add(new CellData
                            {
                                crds = new int2(x, y),
                                isFull = false
                            });
                        }
                    }

                    for (int j = 0; j < shuffle.Length; j++)
                    {
                        CellData temp = shuffle[j];
                        int k = rand.NextInt(0, shuffle.Length - 1);
                        shuffle[j] = shuffle[k];
                        shuffle[k] = temp;
                    }
                }
                arrayOfShuffles[ruleSetIndex] = shuffle;
            }
        }

        /// <summary>
        /// Returns false when all cells are full.
        /// </summary>
        public bool GetNextCell(
            ref int guestRuleSetIndex, 
            ref RulesGridData guestRulesGridData, 
            out int2 guestCrds,
            NativeArray<RulesGridData> rulesGridDataArray)
        {
            bool cellsAreFull = false;
            bool foundAvailableCell = false;
            int priorGuestRuleSetIndex = guestRuleSetIndex;
            while (!cellsAreFull && !foundAvailableCell)
            {
                Increment(ref guestRuleSetIndex, 0, ruleSetCount);
                RulesGridData subSetData = rulesGridDataArray[guestRuleSetIndex];
                if (!subSetData.isEnabled) { continue; }
                UnsafeList<CellData> shuffle = arrayOfShuffles[guestRuleSetIndex];
                int priorIndex = subSetData.currentShuffleIndex;
                while (!subSetData.isFull && !foundAvailableCell)
                {
                    Increment(ref subSetData.currentShuffleIndex, 0, shuffle.Length);
                    bool cellIsFull = shuffle[subSetData.currentShuffleIndex].isFull;
                    subSetData.isFull = subSetData.currentShuffleIndex == priorIndex && cellIsFull;
                    foundAvailableCell = !cellIsFull;
                }
                cellsAreFull = guestRuleSetIndex == priorGuestRuleSetIndex && subSetData.isFull;
                rulesGridDataArray[guestRuleSetIndex] = subSetData;
                arrayOfShuffles[guestRuleSetIndex] = shuffle;
            }
            guestRulesGridData = rulesGridDataArray[guestRuleSetIndex];
            guestCrds = arrayOfShuffles[guestRuleSetIndex][guestRulesGridData.currentShuffleIndex].crds;
            return !cellsAreFull;
        }

        public void MarkCurrentCellAsFull(int guestRuleSetIndex, RulesGridData guestRulesGridData)
        {
            int currentIndex = guestRulesGridData.currentShuffleIndex;
            UnsafeList<CellData> shuffle = arrayOfShuffles[guestRuleSetIndex];
            CellData cellData = shuffle[currentIndex];
            cellData.isFull = true;
            shuffle[currentIndex] = cellData;
            arrayOfShuffles[guestRuleSetIndex] = shuffle;
        }

        void Increment(ref int current, int start, int length)
        {
            current = current + 1 >= start + length ? start : current + 1;
        }

        public void Dispose()
        {
            for (int i = 0; i < arrayOfShuffles.Length; i++)
            {
                arrayOfShuffles[i].Dispose();
            }
            arrayOfShuffles.Dispose();
        }
    }
}