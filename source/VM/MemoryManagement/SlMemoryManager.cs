using System;
using System.Collections.Generic;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM.MemoryManagement
{
    /// <summary>
    /// Dart-inspired SL heap coordinator: nursery promotion, logical generations, tracing GC from VM/static roots.
    /// Does not replace the CLR GC; it manages language-level reachability and metadata on <see cref="SObject"/>.
    /// </summary>
    public sealed class SlMemoryManager
    {
        public static SlMemoryManager Instance { get; } = new SlMemoryManager();

        private readonly object _gate = new object();
        private readonly List<SObject> _nursery = new List<SObject>();
        private readonly List<WeakReference<SObject>> _weakAllocs = new List<WeakReference<SObject>>();
        private readonly HashSet<SObject> _pinned = new HashSet<SObject>();
        private readonly List<ISlMemoryRootProvider> _rootProviders = new List<ISlMemoryRootProvider>();
        private readonly List<WeakReference<RuntimeVM>> _vmRoots = new List<WeakReference<RuntimeVM>>();

        private int _allocSinceYoung;
        private SlMemoryStatistics _stats;

        private SlMemoryManager()
        {
            Options = new SlMemoryOptions();
        }

        public SlMemoryOptions Options { get; set; }

        /// <summary>Optional: extra CLR-side root callback (in addition to registered <see cref="RuntimeVM"/> weak list).</summary>
        public Action<HashSet<SObject>>? GlobalVmRootCollector { get; set; }

        public ISlUnreachableObjectSink? UnreachableSink { get; set; }

        public void RegisterRootProvider(ISlMemoryRootProvider provider)
        {
            lock (_gate)
            {
                if (!_rootProviders.Contains(provider))
                    _rootProviders.Add(provider);
            }
        }

        public void UnregisterRootProvider(ISlMemoryRootProvider provider)
        {
            lock (_gate)
            {
                _rootProviders.Remove(provider);
            }
        }

        /// <summary>Weak-track <see cref="RuntimeVM"/> so stack/locals participate as GC roots without leaking the VM.</summary>
        public void RegisterVmForRootCollection(RuntimeVM vm)
        {
            if (vm == null) return;
            lock (_gate)
            {
                _vmRoots.Add(new WeakReference<RuntimeVM>(vm));
            }
        }

        public void RegisterAllocation(SObject obj)
        {
            if (obj == null) return;

            lock (_gate)
            {
                _stats.TotalRegisteredAllocations++;
                obj.gcGeneration = VMObjectHeader.GcGenYoung;
                _nursery.Add(obj);
                _weakAllocs.Add(new WeakReference<SObject>(obj));
                _allocSinceYoung++;

                if (Options.AutoYoungCollection
                    && Options.NurseryAllocationBudget > 0
                    && _allocSinceYoung >= Options.NurseryAllocationBudget)
                {
                    CollectYoungGenerationUnlocked();
                }
            }
        }

        public SlMemoryStatistics GetStatistics()
        {
            lock (_gate)
            {
                CompactWeakList();
                _stats.NurseryLiveObjects = _nursery.Count;
                _stats.PinnedRoots = _pinned.Count;
                return _stats;
            }
        }

        /// <summary>Manual: young generation pass (nursery sweep + promotion).</summary>
        public void CollectYoungGeneration()
        {
            lock (_gate)
            {
                CollectYoungGenerationUnlocked();
            }
        }

        /// <summary>Manual: full reachability pass over weak-registered allocations.</summary>
        public void CollectFull()
        {
            lock (_gate)
            {
                CollectFullUnlocked();
            }
        }

        public void Pin(SObject obj)
        {
            if (obj == null) return;
            lock (_gate)
            {
                _pinned.Add(obj);
            }
        }

        public void Unpin(SObject obj)
        {
            if (obj == null) return;
            lock (_gate)
            {
                _pinned.Remove(obj);
            }
        }

        public bool IsPinned(SObject obj) => obj != null && _pinned.Contains(obj);

        private void CollectYoungGenerationUnlocked()
        {
            var roots = BuildRootSetUnlocked();
            var marked = SlGarbageCollector.MarkFromRoots(roots, _pinned);

            var unreachable = new List<SObject>();
            var snapshot = _nursery.ToArray();
            _nursery.Clear();

            foreach (var o in snapshot)
            {
                if (o == null) continue;
                if (marked.Contains(o))
                {
                    o.gcGeneration = VMObjectHeader.GcGenOld;
                }
                else
                {
                    unreachable.Add(o);
                }
            }

            _allocSinceYoung = 0;
            _stats.YoungCollections++;
            _stats.LastUnreachableYoung = unreachable.Count;
            _stats.LastCollectionUtc = DateTime.UtcNow;

            if (unreachable.Count > 0 && UnreachableSink != null)
                UnreachableSink.OnUnreachableObjects(unreachable);

            MaybeRequestClrGc();
        }

        private void CollectFullUnlocked()
        {
            CompactWeakList();

            var roots = BuildRootSetUnlocked();
            var marked = SlGarbageCollector.MarkFromRoots(roots, _pinned);

            var snapshotNursery = _nursery.ToArray();
            _nursery.Clear();
            foreach (var o in snapshotNursery)
            {
                if (o == null) continue;
                if (marked.Contains(o))
                    o.gcGeneration = VMObjectHeader.GcGenOld;
            }

            var unreachable = new List<SObject>();
            foreach (var wr in _weakAllocs)
            {
                if (!wr.TryGetTarget(out var o) || o == null)
                    continue;
                if (_pinned.Contains(o)) continue;
                if (marked.Contains(o)) continue;
                unreachable.Add(o);
            }

            _allocSinceYoung = 0;
            _stats.FullCollections++;
            _stats.LastUnreachableYoung = unreachable.Count;
            _stats.LastCollectionUtc = DateTime.UtcNow;

            if (unreachable.Count > 0 && UnreachableSink != null)
                UnreachableSink.OnUnreachableObjects(unreachable);

            MaybeRequestClrGc();
        }

        private void MaybeRequestClrGc()
        {
            if (Options.RequestClrGcAfterSlCollection)
                GC.Collect();
        }

        private HashSet<SObject> BuildRootSetUnlocked()
        {
            var roots = new HashSet<SObject>();
            GlobalVmRootCollector?.Invoke(roots);
            CompactVmRoots();
            foreach (var wr in _vmRoots)
            {
                if (!wr.TryGetTarget(out var vm) || vm == null) continue;
                vm.AppendSlMemoryRoots(roots);
            }
            foreach (var p in _rootProviders)
                p.AppendRoots(roots);
            return roots;
        }

        private void CompactVmRoots()
        {
            _vmRoots.RemoveAll(w => !w.TryGetTarget(out _));
        }

        private void CompactWeakList()
        {
            _weakAllocs.RemoveAll(w => !w.TryGetTarget(out _));
        }
    }
}
