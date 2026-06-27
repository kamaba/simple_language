using System;
using System.Diagnostics;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class MathSystemMethodCall
    {
        #region Trigonometric
        public static void ExecuteMathSin(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Sin(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathCos(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Cos(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathTan(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Tan(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathAsin(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Asin(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathAcos(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Acos(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathAtan(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Atan(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathAtan2(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(2, out var args)) return;
            float y = ReadFloatArg(ref args[0]);
            float x = ReadFloatArg(ref args[1]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Atan2(y, x));
            vm.PushSValueSynced(outv);
        }
        #endregion

        #region Hyperbolic
        public static void ExecuteMathSinh(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Sinh(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathCosh(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Cosh(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathTanh(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Tanh(val));
            vm.PushSValueSynced(outv);
        }
        #endregion

        #region Power / Log
        public static void ExecuteMathPow(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(2, out var args)) return;
            float baseVal = ReadFloatArg(ref args[0]);
            float exp = ReadFloatArg(ref args[1]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Pow(baseVal, exp));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathSqrt(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Sqrt(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathExp(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Exp(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathLog(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Log(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathLog10(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Log10(val));
            vm.PushSValueSynced(outv);
        }
        #endregion

        #region Rounding
        public static void ExecuteMathCeil(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Ceiling(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathFloor(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Floor(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathRound(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetFloatValue((float)Math.Round(val));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteMathTruncate(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            if (!vm.TrySystemCallPopArgs(1, out var args)) return;
            float val = ReadFloatArg(ref args[0]);
            var outv = default(SValue);
            outv.SetInt32Value((int)Math.Truncate(val));
            vm.PushSValueSynced(outv);
        }
        #endregion

        #region Helper
        private static float ReadFloatArg(ref SValue v)
        {
            return v.eType switch
            {
                EVMType.Float32 => v.float32Value,
                EVMType.Float64 => (float)v.float64Value,
                EVMType.Int32 => v.int32Value,
                EVMType.Int64 => (float)v.int64Value,
                _ => Convert.ToSingle(v.GetValueObject() ?? 0),
            };
        }
        #endregion
    }
}
