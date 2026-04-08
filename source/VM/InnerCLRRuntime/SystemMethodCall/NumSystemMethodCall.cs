using System.Diagnostics;
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
            var outv = SystemMethodConvertHelper.ConvertValue(ref args[0], kind);
            vm.PushSValueSynced(outv);
        }
    }
}
