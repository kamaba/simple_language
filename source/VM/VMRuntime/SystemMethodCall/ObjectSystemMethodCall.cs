using System;
using System.Diagnostics;
using System.Globalization;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM.MemoryManagement;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ObjectSystemMethodCall
    {
        public static void ExecuteSystemObjectGetType(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectGetType stack underflow, need={pc}");
                return;
            }

            var a = args[0];
            SObject? sobj = a.GetReferenceSObject(createStringRef: true);
            RuntimeType? rt = sobj?.runtimeType ?? RuntimeTypeManager.GetRuntimeTypeByEVMType(a.eType) ?? RuntimeTypeManager.objectRuntimeType;
            var tobj = RuntimeTypeManager.CreateTypeObject(rt);
            var sv = default(RuntimeValue);
            sv.SetValueBySObject(tobj);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteSystemObjectGetHashCode(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectGetHashCode stack underflow, need={pc}");
                return;
            }

            int hash = 0;
            var a = args[0];
            if (a.isNull)
            {
                hash = 0;
            }
            else if (a.sobject != null)
            {
                hash = a.sobject.id;
            }
            else
            {
                hash = a.GetValueObject()?.GetHashCode() ?? 0;
            }

            var outv = default(RuntimeValue);
            outv.SetInt32Value(hash);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteSystemObjectRef(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            ExecuteSystemObjectRefCore(vm, sysPkg, retainForStrongRef: true);
        }

        public static void ExecuteSystemObjectRefWeak(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            ExecuteSystemObjectRefCore(vm, sysPkg, retainForStrongRef: false);
        }

        /// <summary>
        /// Strong ref: <see cref="ManualMemory.Retain"/> (ties to <see cref="SObject.refCount"/>).
        /// Weak ref: no retain â€?object remains subject to SL GC like Dart weak references.
        /// </summary>
        private static void ExecuteSystemObjectRefCore(RuntimeVM vm, SLSystemMethodCallPackage sysPkg, bool retainForStrongRef)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectRef stack underflow, need={pc}");
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
            {
                sv.SetNull();
            }
            else
            {
                sv.SetValueBySObject(sobj);
                if (retainForStrongRef)
                    ObjectManager.RetainObject(sobj);
            }
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteSystemObjectRefCount(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectRefCount stack underflow, need={pc}");
                return;
            }

            // Same counter as <see cref="ManualMemory.Retain"/> / <see cref="ManualMemory.Release"/>.
            int count = 0;
            var a = args[0];
            if (!a.isNull && a.sobject != null)
            {
                count = a.sobject.refCount;
            }

            var outv = default(RuntimeValue);
            outv.SetInt32Value(count);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteSystemObjectFree(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectFree stack underflow, need={pc}");
                return;
            }

            var a = args[0];
            if (!a.isNull && a.sobject != null)
            {
                ManualMemory.Unpin(a.sobject);
                a.sobject.refCount = 0;
                ObjectManager.OnManualRefForcedZero(a.sobject);
            }
        }

        public static void ExecuteSystemObjectRelease(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemObjectRelease stack underflow, need={pc}");
                return;
            }

            var a = args[0];
            if (!a.isNull && a.sobject != null)
                ObjectManager.ReleaseObject(a.sobject);
        }

        public static void ExecuteSystemEqualObject(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemEqualObject stack underflow, need={pc}");
                return;
            }

            bool eq = SystemBuiltinEqualObject(ref args[0], ref args[1]);
            var outv = default(RuntimeValue);
            outv.SetBoolValue(eq);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteSystemArrayGetValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayGetValueThis stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            if (arrObj == null)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            int index = 0;
            try { index = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            var sv = default(RuntimeValue);
            arrObj.LoadValue(index, ref sv );
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteSystemArraySetValueThis(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArraySetValueThis stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            if (arrObj == null) return;

            int index = 0;
            var idxArg = args[1];
            idxArg.TryNormalizeObjectScalarInPlace();
            try { index = Convert.ToInt32(idxArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            var value = args[2];
            var elementRt = arrObj.runtimeType?.runtimeTemplateList;
            if (elementRt != null && elementRt.Count > 0)
            {
                var targetEvm = elementRt[0].eType;
                value.TryNormalizeObjectScalarInPlace();
                value.TryCoerceScalarForAssignment(targetEvm);
            }

            arrObj.StoreValue(index, value);
        }

        /// <summary>Fast object equality for system builtin <see cref="ESystemMethodCall.SystemEqualObject"/>.</summary>
        private static bool SystemBuiltinEqualObject(ref RuntimeValue a, ref RuntimeValue b)
        {
            if (a.isNull && b.isNull) return true;
            if (a.isNull || b.isNull) return false;

            if (ReferenceEquals(a.sobject, b.sobject)) return true;
            if (a.sobject != null && b.sobject != null)
            {
                return a.sobject == b.sobject;
            }

            object? av = a.GetValueObject();
            object? bv = b.GetValueObject();
            if (ReferenceEquals(av, bv)) return true;
            if (av == null || bv == null) return false;
            return Equals(av, bv);
        }
    }
}
