//****************************************************************************
//  ExternalFunctionSystemMethodCall: 处理 SystemCallExternalFunction。
//  第一个参数是函数名（字符串），后续参数是实际调用参数。
//  从 VMExternalFunctionRegistry 查找并调用已注册的外部函数。
//****************************************************************************

using System;
using System.Diagnostics;
using SimpleLanguage.Logging;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ExternalFunctionSystemMethodCall
    {
        public static void Execute(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int paramCount = sysPkg.paramCount;
            if (paramCount < 1)
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "SystemCallExternalFunction: requires at least 1 argument (function name)");
                return;
            }

            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemCallExternalFunction stack underflow, need={paramCount}");
                return;
            }

            // 第一个参数是函数名
            var funcNameValue = args[0];
            string funcName = funcNameValue.stringValue ?? funcNameValue.GetValueObject()?.ToString() ?? "";

            if (string.IsNullOrEmpty(funcName))
            {
                Log.AddRuntimeLog(LID.ShowMessageAssert, "SystemCallExternalFunction: function name is empty");
                return;
            }

            // 剩余参数是实际调用参数
            int argCount = paramCount - 1;
            object?[] externalArgs = new object?[argCount];
            for (int i = 0; i < argCount; i++)
            {
                externalArgs[i] = args[i + 1].GetValueObject();
            }

            // 查找并调用
            if (!VMExternalFunctionRegistry.TryInvoke(funcName, externalArgs, out var result))
            {
                Log.AddRuntimeLog(LID.ShowMessageWarning, $"SystemCallExternalFunction: function '{funcName}' not found in registry (registered={VMExternalFunctionRegistry.FunctionCount})");
                vm.PushSValueSynced(default(RuntimeValue));
                return;
            }

            // 将返回值压回 VM 栈
            var rv = RuntimeValue.FromClrObject(result);
            vm.PushSValueSynced(rv);
        }
    }
}
