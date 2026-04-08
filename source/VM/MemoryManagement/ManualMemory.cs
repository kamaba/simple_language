using SimpleLanguage.VM;

namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>Manual SL memory controls (pinning, explicit retain/release, GC requests).</summary>
    public static class ManualMemory
    {
        public static void Pin(SObject obj) => SlMemoryManager.Instance.Pin(obj);

        public static void Unpin(SObject obj) => SlMemoryManager.Instance.Unpin(obj);

        public static bool IsPinned(SObject obj) => SlMemoryManager.Instance.IsPinned(obj);

        /// <summary>Maps to <see cref="SObject.refCount"/> — explicit reference accounting alongside GC.</summary>
        public static void Retain(SObject obj)
        {
            if (obj == null) return;
            obj.refCount++;
        }

        /// <summary>
        /// Decrements <see cref="SObject.refCount"/> (pairs with <see cref="Retain"/> and <c>SystemObjectRef</c>).
        /// When the count reaches zero, removes a <see cref="ClassObject"/> from <see cref="ObjectManager.classObjectDict"/> if present (aligned with legacy <c>ObjectClass.FreeObject</c>).
        /// Heap reclamation is still done by SL tracing GC, not here. Pinning is independent: use <see cref="Unpin"/> or <c>SystemObjectFree</c> to leave the pin set.
        /// </summary>
        public static void Release(SObject obj)
        {
            if (obj == null) return;
            if (obj.refCount <= 0) return;
            obj.refCount--;
            if (obj.refCount != 0) return;
            TryRemoveClassObjectRegistryEntry(obj);
        }

        /// <summary>Same <see cref="ClassObject"/> registry cleanup as when <see cref="Release"/> reaches zero, for paths that assign <see cref="SObject.refCount"/> to zero directly (e.g. <c>SystemObjectFree</c>).</summary>
        public static void OnManualRefForcedZero(SObject obj) => TryRemoveClassObjectRegistryEntry(obj);

        private static void TryRemoveClassObjectRegistryEntry(SObject sobj)
        {
            if (sobj is not ClassObject co) return;
            try
            {
                int key = co.GetHashCode();
                if (ObjectManager.classObjectDict.ContainsKey(key))
                    ObjectManager.classObjectDict.Remove(key);
            }
            catch
            {
                // same as legacy FreeObject: never throw from refcount cleanup
            }
        }

        public static void CollectYoungGeneration() => SlMemoryManager.Instance.CollectYoungGeneration();

        public static void CollectFull() => SlMemoryManager.Instance.CollectFull();

        public static SlMemoryStatistics GetStatistics() => SlMemoryManager.Instance.GetStatistics();
    }
}
