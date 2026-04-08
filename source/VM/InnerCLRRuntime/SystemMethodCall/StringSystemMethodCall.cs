using System.Diagnostics;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class StringSystemMethodCall
    {
        public static void ExecuteStringConvert(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemConvertString stack underflow, need={pc}");
                return;
            }
            var outv = SystemMethodConvertHelper.ConvertValue(ref args[0], ESystemMethodCall.SystemConvertString);
            vm.PushSValueSynced(outv);
        }
    }
}
