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
                hash = a.sobject.hashCode;
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
        /// Weak ref: no retain �?object remains subject to SL GC like Dart weak references.
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

        public static void ExecuteSystemArrayFillValue(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 4 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayFillValue stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            if (arrObj == null) return;

            int startIndex = 0;
            var startArg = args[1];
            startArg.TryNormalizeObjectScalarInPlace();
            try { startIndex = Convert.ToInt32(startArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { startIndex = 0; }

            int length = 0;
            var lenArg = args[2];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            var value = args[3];
            var elementRt = arrObj.runtimeType?.runtimeTemplateList;
            if (elementRt != null && elementRt.Count > 0)
            {
                var targetEvm = elementRt[0].eType;
                value.TryNormalizeObjectScalarInPlace();
                value.TryCoerceScalarForAssignment(targetEvm);
            }

            // 与 List.fill( value, startIndex, count ) 的区间语义对应：
            // startIndex 越界或 count 无效时不填充；count 超出数组剩余槽位时截断到数组末尾。
            if (startIndex < 0 || startIndex >= arrObj.length || length <= 0) return;
            int end = startIndex + length;
            if (end > arrObj.length) end = arrObj.length;
            for (int i = startIndex; i < end; i++)
            {
                arrObj.StoreValue(i, value);
            }
        }

        /// <summary>Common tail of Resize/Copy: allocate a new array with the same element
        /// runtime type and copy the first <paramref name="count"/> elements, then push it.</summary>
        private static void CopyArrayPrefix(RuntimeVM vm, ArrayObject arrObj, int newLength, int count)
        {
            if (newLength < 0) newLength = 0;
            if (count < 0) count = 0;
            if (count > arrObj.length) count = arrObj.length;
            if (count > newLength) count = newLength;

            var newArr = new ArrayObject(arrObj.runtimeType, newLength);
            newArr.CreateObject();
            ObjectManager.AddClassObject(newArr);
            for (int i = 0; i < count; i++)
            {
                var sv = default(RuntimeValue);
                arrObj.LoadValue(i, ref sv);
                newArr.StoreValue(i, sv);
            }

            var outv = default(RuntimeValue);
            outv.SetValueBySObject(newArr);
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteSystemArrayResize(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayResize stack underflow, need={pc}");
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

            int newCapacity = 0;
            var capArg = args[1];
            capArg.TryNormalizeObjectScalarInPlace();
            try { newCapacity = Convert.ToInt32(capArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { newCapacity = 0; }

            CopyArrayPrefix(vm, arrObj, newCapacity, int.MaxValue);
        }

        public static void ExecuteSystemArrayInsertValue(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 4 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayInsertValue stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            if (arrObj == null) return;

            int index = 0;
            var idxArg = args[1];
            idxArg.TryNormalizeObjectScalarInPlace();
            try { index = Convert.ToInt32(idxArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            int length = 0;
            var lenArg = args[2];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            var value = args[3];
            var elementRt = arrObj.runtimeType?.runtimeTemplateList;
            if (elementRt != null && elementRt.Count > 0)
            {
                var targetEvm = elementRt[0].eType;
                value.TryNormalizeObjectScalarInPlace();
                value.TryCoerceScalarForAssignment(targetEvm);
            }

            if (index < 0) return;
            // Mirrors List.insert: shift [index, length) right by one, then store value at index.
            // StoreValue/LoadValue silently ignore out-of-range slots (same as the old SL loop).
            for (int i = length; i > index; i--)
            {
                var sv = default(RuntimeValue);
                arrObj.LoadValue(i - 1, ref sv);
                arrObj.StoreValue(i, sv);
            }
            arrObj.StoreValue(index, value);
        }

        public static void ExecuteSystemArrayRemoveAtValue(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayRemoveAtValue stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            if (arrObj == null) return;

            int index = 0;
            var idxArg = args[1];
            idxArg.TryNormalizeObjectScalarInPlace();
            try { index = Convert.ToInt32(idxArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            int length = 0;
            var lenArg = args[2];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            if (index < 0 || index >= length) return;
            // Mirrors List.removeAt: shift (index, length) left by one, then clear the last slot.
            for (int i = index; i < length - 1; i++)
            {
                var sv = default(RuntimeValue);
                arrObj.LoadValue(i + 1, ref sv);
                arrObj.StoreValue(i, sv);
            }
            var nv = default(RuntimeValue);
            nv.SetNull();
            arrObj.StoreValue(length - 1, nv);
        }

        public static void ExecuteSystemArrayRemoveValue(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayRemoveValue stack underflow, need={pc}");
                return;
            }

            var arrObj = args[0].sobject as ArrayObject;
            var item = args[1];

            int length = 0;
            var lenArg = args[2];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            var outv = default(RuntimeValue);
            outv.SetInt32Value(-1);

            if (arrObj != null)
            {
                if (length < 0) length = 0;
                if (length > arrObj.length) length = arrObj.length;
                // Mirrors List.remove: find the first slot equal to item, shift the tail left by
                // one, clear the last slot, and return its index (-1 when not found).
                for (int i = 0; i < length; i++)
                {
                    var cur = default(RuntimeValue);
                    arrObj.LoadValue(i, ref cur);
                    if (!SystemBuiltinEqualObject(ref cur, ref item)) continue;

                    for (int j = i; j < length - 1; j++)
                    {
                        var sv = default(RuntimeValue);
                        arrObj.LoadValue(j + 1, ref sv);
                        arrObj.StoreValue(j, sv);
                    }
                    var nv = default(RuntimeValue);
                    nv.SetNull();
                    arrObj.StoreValue(length - 1, nv);
                    outv.SetInt32Value(i);
                    break;
                }
            }

            vm.PushSValueSynced(outv);
        }

        public static void ExecuteSystemArrayCopy(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemArrayCopy stack underflow, need={pc}");
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

            int length = 0;
            var lenArg = args[1];
            lenArg.TryNormalizeObjectScalarInPlace();
            try { length = Convert.ToInt32(lenArg.GetValueObject(), CultureInfo.InvariantCulture); }
            catch { length = 0; }

            CopyArrayPrefix(vm, arrObj, length, length);
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
