//****************************************************************************
//  External function module interface.
//  External DLLs implement ISLExternalFunctionModule to register
//  native functions that can be called from .sl code via
//  SystemCallExternalFunction.
//****************************************************************************

using System;

namespace SimpleLanguage.VM.Runtime
{
    /// <summary>
    /// 外部 DLL 模块接口。DLL 中的类实现此接口，VM 加载 DLL 时
    /// 会找到所有实现此接口的类型并调用 Register 方法。
    /// </summary>
    public interface ISLExternalFunctionModule
    {
        /// <summary>
        /// 注册外部函数。通过 context.Register(name, fn) 注册。
        /// </summary>
        void Register(ISLExternalFunctionRegistrar context);
    }

    /// <summary>
    /// 注册器接口，DLL 通过它注册函数。
    /// </summary>
    public interface ISLExternalFunctionRegistrar
    {
        /// <summary>
        /// 注册一个外部函数。name 为 .sl 中调用的函数名（如 "Console.println"）。
        /// </summary>
        void Register(string name, SLExternalFunctionDelegate fn);
    }

    /// <summary>
    /// 外部函数委托签名。
    /// args 为调用参数（已从 VM 栈弹出），返回值会被压回 VM 栈。
    /// 返回 null 表示无返回值（void）。
    /// </summary>
    /// <param name="args">参数数组</param>
    /// <returns>返回值，null 表示 void</returns>
    public delegate object? SLExternalFunctionDelegate(object?[] args);
}
