using System;

namespace SimpleLanguage.VM.MemoryManagement
{
    public struct SlMemoryStatistics
    {
        public long TotalRegisteredAllocations;
        public long YoungCollections;
        public long FullCollections;
        public int NurseryLiveObjects;
        public int PinnedRoots;
        public int LastUnreachableYoung;
        public DateTime LastCollectionUtc;
    }
}
