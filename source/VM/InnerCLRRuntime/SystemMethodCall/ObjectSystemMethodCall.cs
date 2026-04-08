using System;
using System.Diagnostics;
using System.Globalization;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ObjectSystemMethodCall
    {
        public static void ExecuteSystemEqualObject(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemEqualObject stack underflow, need={pc}");
                return;
            }

            bool eq = SystemBuiltinEqualObject(ref args[0], ref args[1]);
            var outv = default(SValue);
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
                var nz = default(SValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            int index = 0;
            try { index = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            var got = arrObj.GetValue(index);
            if (got is SObject so)
            {
                var sv = default(SValue);
                vm.SetSValue(so, so.eType, ref sv);
                vm.PushSValueSynced(sv);
            }
            else
            {
                vm.PushSValueSynced(SValue.FromClrObject(got));
            }
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
            try { index = Convert.ToInt32(args[1].GetValueObject(), CultureInfo.InvariantCulture); }
            catch { index = 0; }

            arrObj.StoreValue(index, args[2]);
        }

        /// <summary>Fast object equality for system builtin <see cref="ESystemMethodCall.SystemEqualObject"/>.</summary>
        private static bool SystemBuiltinEqualObject(ref SValue a, ref SValue b)
        {
            if (a.isNull && b.isNull) return true;
            if (a.isNull || b.isNull) return false;

            if (ReferenceEquals(a.sobject, b.sobject)) return true;
            if (a.sobject != null && b.sobject != null) return false;

            object? av = a.GetValueObject();
            object? bv = b.GetValueObject();
            if (ReferenceEquals(av, bv)) return true;
            if (av == null || bv == null) return false;
            return Equals(av, bv);
        }
    }
}
