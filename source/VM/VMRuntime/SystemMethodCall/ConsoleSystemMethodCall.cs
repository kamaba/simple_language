using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM;

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
                VmRunResultSink.MirrorConsole(string.Empty, newLine: false);
                return;
            }
            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemPrint stack underflow, need={paramCount}");
                return;
            }

            var text = FormatConsoleValue(ref args[0]);
            Console.Write(text);
            VmRunResultSink.MirrorConsole(text, newLine: false);
        }

        public static void ExecuteSystemPrintln(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int paramCount = sysPkg.paramCount;
            if (paramCount <= 0)
            {
                Console.WriteLine();
                VmRunResultSink.MirrorConsole(null, newLine: true);
                return;
            }
            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemPrintln stack underflow, need={paramCount}");
                return;
            }

            var text = FormatConsoleValue(ref args[0]);
            Console.WriteLine(text);
            VmRunResultSink.MirrorConsole(text, newLine: true);
        }

        private static string FormatConsoleValue(ref RuntimeValue value)
        {
            if (value.isNull)
                return string.Empty;

            if (DataSystemMethodCall.TryBuildDataString(ref value, out var dataText))
                return dataText;

            var textObj = value.GetValueObject();
            return textObj?.ToString() ?? string.Empty;
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
            var sv = default(RuntimeValue);
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
            var svk = default(RuntimeValue);
            svk.SetStringValue(k.KeyChar.ToString());
            vm.PushSValueSynced(svk);
        }

        /// <summary>
        /// SystemInput: 从标准输入读取一行（直到回车），返回 string。
        /// 与 SystemReadLine 行为一致，语义上强调"读到回车为止"。
        /// </summary>
        public static void ExecuteSystemInput(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopDiscard(pc))
            {
                Debug.Assert(false, $"SystemInput stack underflow, need={pc}");
                return;
            }
            string line = Console.ReadLine() ?? string.Empty;
            var sv = default(RuntimeValue);
            sv.SetStringValue(line);
            vm.PushSValueSynced(sv);
        }
    }
}
