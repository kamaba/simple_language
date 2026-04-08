using System;
using System.Diagnostics;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ConsoleSystemMethodCall
    {
        public static void ExecuteSystemPrint(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int paramCount = sysPkg.paramCount;
            if (paramCount <= 0)
            {
                Console.Write(string.Empty);
                return;
            }
            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemPrint stack underflow, need={paramCount}");
                return;
            }

            var textObj = args[0].GetValueObject();
            var text = textObj?.ToString() ?? string.Empty;
            Console.Write(text);
        }

        public static void ExecuteSystemReadLine(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopDiscard(pc))
            {
                Debug.Assert(false, $"SystemReadLine stack underflow, need={pc}");
                return;
            }
            string line = Console.ReadLine() ?? string.Empty;
            var sv = default(SValue);
            sv.SetStringValue(line);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteSystemReadKey(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopDiscard(pc))
            {
                Debug.Assert(false, $"SystemReadKey stack underflow, need={pc}");
                return;
            }
            var k = Console.ReadKey(intercept: true);
            var svk = default(SValue);
            svk.SetStringValue(k.KeyChar.ToString());
            vm.PushSValueSynced(svk);
        }
    }
}
