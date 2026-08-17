using System.Diagnostics;
using System;
using System.Globalization;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class NumSystemMethodCall
    {
        /// <summary>Numeric <see cref="ESystemMethodCall"/> converts (Int8 �?Float64).</summary>
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
                RuntimeValue outv;
                if (pc == 1)
                    outv = SystemMethodConvertHelper.ConvertInt8(ref args[0], -1);
                else if (pc == 2)
                    outv = SystemMethodConvertHelper.ConvertInt8(ref args[0], SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[1]));
                else
                {
                    Debug.Assert(false, $"SystemConvertInt8/SystemConvertUInt8 expects 1 or 2 args, got {pc}");
                    var z = default(RuntimeValue);
                    z.SetNull();
                    outv = z;
                }

                vm.PushSValueSynced(outv);
                return;
            }

            if (kind == ESystemMethodCall.SystemConvertSInt8)
            {
                RuntimeValue outv;
                if (pc == 1)
                    outv = SystemMethodConvertHelper.ConvertSInt8(ref args[0], -1);
                else if (pc == 2)
                    outv = SystemMethodConvertHelper.ConvertSInt8(ref args[0], SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[1]));
                else
                {
                    Debug.Assert(false, $"SystemConvertSInt8 expects 1 or 2 args, got {pc}");
                    var z = default(RuntimeValue);
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
                var outv = default(RuntimeValue);
                outv.SetInt32Value(parsed);
                vm.PushSValueSynced(outv);
            }
            catch
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
            }
        }

        public static void ExecuteSystemConvertInt32ToRadixString(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemConvertInt32ToRadixString stack underflow, need={pc}");
                return;
            }

            int value = SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[0]);
            int radix = pc > 1 ? SystemMethodConvertHelper.ReadInt32ArgLoose(ref args[1]) : 10;

            string result = ConvertInt32ToRadixString(value, radix);
            var outv = default(RuntimeValue);
            outv.SetStringValue(result);
            vm.PushSValueSynced(outv);
        }

        /// <summary>Convert a signed 32-bit integer to its string in the given radix (2..36).
        /// Radix 10 keeps the natural signed representation (e.g. -5 -> "-5"); other radices
        /// treat the value as an unsigned bit pattern (e.g. -1 in hex -> "ffffffff"), matching
        /// the conventional semantics of <c>Convert.ToString(int, int)</c>.</summary>
        private static string ConvertInt32ToRadixString(int value, int radix)
        {
            if (radix < 2 || radix > 36)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
            if (radix == 10)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            // For non-decimal radices use the raw bit pattern as an unsigned magnitude so
            // negative values render as two's-complement (e.g. -1 -> "1111..1" / "ffffffff").
            uint mag = unchecked((uint)value);

            char[] buf = new char[32];
            int pos = 32;
            while (mag != 0)
            {
                uint rem = mag % (uint)radix;
                buf[--pos] = digits[(int)rem];
                mag /= (uint)radix;
            }
            if (pos == 32)
            {
                buf[--pos] = '0';
            }
            return new string(buf, pos, 32 - pos);
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
                var absSv = RuntimeValue.FromClrObject(abs);
                var preferKind = InferPreferredConvertKind(ref args[0]);
                var outv = SystemMethodConvertHelper.ConvertValue(ref absSv, preferKind);
                vm.PushSValueSynced(outv);
            }
            catch
            {
                var nz = default(RuntimeValue);
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
                vm.PushSValueSynced(RuntimeValue.FromClrObject(floor));
            }
            catch
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
            }
        }

        private static ESystemMethodCall InferPreferredConvertKind(ref RuntimeValue arg)
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
                switch (arg.sobject.eType)
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
            }

            return ESystemMethodCall.SystemConvertFloat64;
        }
    }
}
