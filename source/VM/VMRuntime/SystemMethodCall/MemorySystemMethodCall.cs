using System.Diagnostics;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM.MemoryManagement;

namespace SimpleLanguage.VM.Runtime
{
    /// <summary>
    /// Handlers for Memory.sl system calls (SystemMemory*).
    /// Mirrors the C VM's vm_sys_mem_* handlers in vm_system_registry.c.
    /// </summary>
    internal static class MemorySystemMethodCall
    {
        // ---- Reference counting ----

        public static void ExecuteMemoryRefCount(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryRefCount stack underflow, need={pc}");
                return;
            }
            int count = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                count = a.sobject.refCount;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(count);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryRetain(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryRetain stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                ObjectManager.RetainObject(a.sobject);
            var outv = default(RuntimeValue);
            outv.SetInt32Value(1);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryFree(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryFree stack underflow, need={pc}");
                return;
            }
            int result = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
            {
                // Only allow free if object is in manual (pinned) mode.
                if (ManualMemory.IsPinned(a.sobject))
                {
                    ManualMemory.Unpin(a.sobject);
                    a.sobject.refCount = 0;
                    ObjectManager.OnManualRefForcedZero(a.sobject);
                    result = 1;
                }
            }
            var outv = default(RuntimeValue);
            outv.SetInt32Value(result);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryRelease(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryRelease stack underflow, need={pc}");
                return;
            }
            int result = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
            {
                if (ManualMemory.IsPinned(a.sobject))
                {
                    ObjectManager.ReleaseObject(a.sobject);
                    result = 1;
                }
            }
            var outv = default(RuntimeValue);
            outv.SetInt32Value(result);
            vm.PushSValueSynced(outv);
        }

        // ---- Per-object mode control ----

        public static void ExecuteMemoryManual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryManual stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                ManualMemory.Pin(a.sobject);
            var outv = default(RuntimeValue);
            outv.SetInt32Value(1);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryAuto(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryAuto stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                ManualMemory.Unpin(a.sobject);
            var outv = default(RuntimeValue);
            outv.SetInt32Value(1);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryIsManual(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryIsManual stack underflow, need={pc}");
                return;
            }
            int result = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                result = ManualMemory.IsPinned(a.sobject) ? 1 : 0;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(result);
            vm.PushSValueSynced(outv);
        }

        // ---- GC control ----

        public static void ExecuteMemoryCollect(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryCollect stack underflow");
                return;
            }
            var statsBefore = ManualMemory.GetStatistics();
            ManualMemory.CollectFull();
            var statsAfter = ManualMemory.GetStatistics();
            int freed = statsAfter.LastUnreachableYoung;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(freed);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryCollectThreshold(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryCollectThreshold stack underflow, need={pc}");
                return;
            }
            int threshold = 0;
            try { threshold = System.Convert.ToInt32(args[0].GetValueObject()); }
            catch { threshold = 0; }

            int freed = 0;
            var stats = ManualMemory.GetStatistics();
            if (stats.NurseryLiveObjects >= threshold)
            {
                ManualMemory.CollectFull();
                var statsAfter = ManualMemory.GetStatistics();
                freed = statsAfter.LastUnreachableYoung;
            }
            var outv = default(RuntimeValue);
            outv.SetInt32Value(freed);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemorySetGcThreshold(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemorySetGcThreshold stack underflow, need={pc}");
                return;
            }
            int threshold = 0;
            try { threshold = System.Convert.ToInt32(args[0].GetValueObject()); }
            catch { threshold = 0; }

            SlMemoryManager.Instance.Options.NurseryAllocationBudget = threshold;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(1);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryGetGcThreshold(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetGcThreshold stack underflow");
                return;
            }
            int threshold = SlMemoryManager.Instance.Options.NurseryAllocationBudget;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(threshold);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemorySetMode(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemorySetMode stack underflow, need={pc}");
                return;
            }
            int mode = 0;
            try { mode = System.Convert.ToInt32(args[0].GetValueObject()); }
            catch { mode = 0; }

            // mode != 0 => GC enabled (auto collection), mode == 0 => manual only.
            SlMemoryManager.Instance.Options.AutoYoungCollection = (mode != 0);
            var outv = default(RuntimeValue);
            outv.SetInt32Value(1);
            vm.PushSValueSynced(outv);
        }

        // ---- Statistics ----

        public static void ExecuteMemoryGetObjectCount(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetObjectCount stack underflow");
                return;
            }
            var stats = ManualMemory.GetStatistics();
            var outv = default(RuntimeValue);
            outv.SetInt32Value(stats.NurseryLiveObjects);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryGetGcCycleCount(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetGcCycleCount stack underflow");
                return;
            }
            var stats = ManualMemory.GetStatistics();
            int cycles = (int)(stats.YoungCollections + stats.FullCollections);
            var outv = default(RuntimeValue);
            outv.SetInt32Value(cycles);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryGetGcFreedCount(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetGcFreedCount stack underflow");
                return;
            }
            var stats = ManualMemory.GetStatistics();
            var outv = default(RuntimeValue);
            outv.SetInt32Value(stats.LastUnreachableYoung);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryGetTotalAllocated(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetTotalAllocated stack underflow");
                return;
            }
            var stats = ManualMemory.GetStatistics();
            var outv = default(RuntimeValue);
            outv.SetInt32Value((int)stats.TotalRegisteredAllocations);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMemoryGetTotalFreed(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 0 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryGetTotalFreed stack underflow");
                return;
            }
            // C# VM doesn't track cumulative freed count; approximate with 0.
            var outv = default(RuntimeValue);
            outv.SetInt32Value(0);
            vm.PushSValueSynced(outv);
        }

        // ---- Weak references ----

        public static void ExecuteMemoryWeakRef(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryWeakRef stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (a.isNull)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }
            var sv = default(RuntimeValue);
            var sobj = a.GetReferenceSObject(createStringRef: true);
            if (sobj == null)
                sv.SetNull();
            else
                sv.SetValueBySObject(sobj);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteMemoryIsWeakRefValid(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryIsWeakRefValid stack underflow, need={pc}");
                return;
            }
            // In the C# VM, the object pointer IS the weak ref handle.
            // It's valid if the object is still alive (not null and sobject != null).
            int result = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                result = 1;
            var outv = default(RuntimeValue);
            outv.SetInt32Value(result);
            vm.PushSValueSynced(outv);
        }

        // ---- KeepAlive ----

        public static void ExecuteMemoryKeepAlive(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryKeepAlive stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (!a.isNull && a.sobject != null)
                ObjectManager.RetainObject(a.sobject);
            // Push the object back (KeepAlive is identity at the call site).
            vm.PushSValueSynced(a);
        }

        // ---- Clone ----

        public static void ExecuteMemoryClone(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemMemoryClone stack underflow, need={pc}");
                return;
            }
            var a = args[0];
            if (a.isNull)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            // Shallow clone: create a new SObject with the same runtime type
            // and copy member data.  For scalar values, just push a copy.
            var sobj = a.GetReferenceSObject(createStringRef: true);
            if (sobj == null)
            {
                // Scalar value: push a copy.
                vm.PushSValueSynced(a);
                return;
            }

            // For class objects: create a new instance with the same type.
            var rt = sobj.runtimeType;
            if (rt == null)
            {
                vm.PushSValueSynced(a);
                return;
            }

            var clone = ObjectManager.CreateObjectByRuntimeType(rt, true);
            if (clone == null)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            // Shallow copy member data (byte-for-byte).
            if (sobj.memberData != null && clone.memberData != null
                && sobj.memberData.Length > 0)
            {
                System.Array.Copy(sobj.memberData, clone.memberData,
                    System.Math.Min(sobj.memberData.Length, clone.memberData.Length));
            }

            SlMemoryManager.Instance.RegisterAllocation(clone);

            var sv = default(RuntimeValue);
            sv.SetValueBySObject(clone);
            vm.PushSValueSynced(sv);
        }
    }
}
