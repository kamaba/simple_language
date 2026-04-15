using System.Diagnostics;
using System;
using System.Globalization;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class NumSystemMethodCall
    {
        /// <summary>Numeric <see cref="ESystemMethodCall"/> converts (Int8 … Float64).</summary>
        public static void ExecuteNumericConvert(RuntimeVM vm, SLSystemMethodCallPackage sysPkg, ESystemMethodCall kind)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemConvert stack underflow, need={pc}");
                return;
            }

            if (kind == ESystemMethodCall.SystemConvertInt8 || kind == ESystemMethodCall.SystemConvertUInt8)
            {
                SValue outv;
                if (pc == 1)
                    outv = SystemMethodConvertHelper.ConvertInt8(ref args[0], -1);
                else if (pc == 2)
                    outv = SystemMethodConvertHelper.ConvertInt8(ref args[0], SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[1]));
                else
                {
                    Debug.Assert(false, $"SystemConvertInt8/SystemConvertUInt8 expects 1 or 2 args, got {pc}");
                    var z = default(SValue);
                    z.SetNull();
                    outv = z;
                }

                vm.PushSValueSynced(outv);
                return;
            }

            if (kind == ESystemMethodCall.SystemConvertSInt8)
            {
                SValue outv;
                if (pc == 1)
                    outv = SystemMethodConvertHelper.ConvertSInt8(ref args[0], -1);
                else if (pc == 2)
                    outv = SystemMethodConvertHelper.ConvertSInt8(ref args[0], SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[1]));
                else
                {
                    Debug.Assert(false, $"SystemConvertSInt8 expects 1 or 2 args, got {pc}");
                    var z = default(SValue);
                    z.SetNull();
                    outv = z;
                }

                vm.PushSValueSynced(outv);
                return;
            }

            var outv2 = SystemMethodConvertHelper.ConvertValue(ref args[0], kind);
            vm.PushSValueSynced(outv2);
        }

        public static void ExecuteSystemInt32Parse(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemInt32Parse stack underflow, need={pc}");
                return;
            }

            try
            {
                object? raw = args[0].GetValueObject();
                int parsed = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                var outv = default(SValue);
                outv.SetInt32Value(parsed);
                vm.PushSValueSynced(outv);
            }
            catch
            {
                var nz = default(SValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
            }
        }

        public static void ExecuteSystemNumAbs(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemNumAbs stack underflow, need={pc}");
                return;
            }

            try
            {
                // 1) normalize to Float64 for math operation
                var asFloat = SystemMethodConvertHelper.ConvertValue(ref args[0], ESystemMethodCall.SystemConvertFloat64);
                double n = Convert.ToDouble(asFloat.GetValueObject(), CultureInfo.InvariantCulture);
                double abs = Math.Abs(n);

                // 2) convert back to caller numeric shape (Byte/Int16/Int32/Int64/Float32/...)
                var absSv = SValue.FromClrObject(abs);
                var preferKind = InferPreferredConvertKind(ref args[0]);
                var outv = SystemMethodConvertHelper.ConvertValue(ref absSv, preferKind);
                vm.PushSValueSynced(outv);
            }
            catch
            {
                var nz = default(SValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
            }
        }

        public static void ExecuteSystemNumFloor(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemNumFloor stack underflow, need={pc}");
                return;
            }

            // Integral operands are already floor'ed.
            switch (args[0].eType)
            {
                case EVMType.Boolean:
                case EVMType.UInt8:
                case EVMType.Int8:
                case EVMType.Int16:
                case EVMType.UInt16:
                case EVMType.Int32:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                    vm.PushSValueSynced(args[0]);
                    return;
            }

            try
            {
                double n = Convert.ToDouble(args[0].GetValueObject(), CultureInfo.InvariantCulture);
                double floor = Math.Floor(n);
                vm.PushSValueSynced(SValue.FromClrObject(floor));
            }
            catch
            {
                var nz = default(SValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
            }
        }

        private static ESystemMethodCall InferPreferredConvertKind(ref SValue arg)
        {
            switch (arg.eType)
            {
                case EVMType.UInt8: return ESystemMethodCall.SystemConvertUInt8;
                case EVMType.Int8: return ESystemMethodCall.SystemConvertSInt8;
                case EVMType.Int16: return ESystemMethodCall.SystemConvertInt16;
                case EVMType.UInt16: return ESystemMethodCall.SystemConvertUInt16;
                case EVMType.Int32: return ESystemMethodCall.SystemConvertInt32;
                case EVMType.UInt32: return ESystemMethodCall.SystemConvertUInt32;
                case EVMType.Int64: return ESystemMethodCall.SystemConvertInt64;
                case EVMType.UInt64: return ESystemMethodCall.SystemConvertUInt64;
                case EVMType.Float32: return ESystemMethodCall.SystemConvertFloat32;
                case EVMType.Float64:
                case EVMType.Num: return ESystemMethodCall.SystemConvertFloat64;
            }

            if (arg.sobject != null)
            {
                switch (arg.sobject)
                {
                    case UInt8Object: return ESystemMethodCall.SystemConvertUInt8;
                    case Int8Object: return ESystemMethodCall.SystemConvertSInt8;
                    case Int16Object: return ESystemMethodCall.SystemConvertInt16;
                    case UInt16Object: return ESystemMethodCall.SystemConvertUInt16;
                    case Int32Object: return ESystemMethodCall.SystemConvertInt32;
                    case UInt32Object: return ESystemMethodCall.SystemConvertUInt32;
                    case Int64Object: return ESystemMethodCall.SystemConvertInt64;
                    case UInt64Object: return ESystemMethodCall.SystemConvertUInt64;
                    case Float32Object: return ESystemMethodCall.SystemConvertFloat32;
                    case Float64Object:
                    case NumObject: return ESystemMethodCall.SystemConvertFloat64;
                }
            }

            return ESystemMethodCall.SystemConvertFloat64;
        }
    }
}
