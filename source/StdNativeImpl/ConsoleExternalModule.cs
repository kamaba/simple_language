//****************************************************************************
//  StdNativeImpl: Console 外部函数实现示例。
//
//  此 DLL 被 VM 动态加载后，ISLExternalFunctionModule.Register 会被调用，
//  注册的函数可被 .sl 中的 SystemCallExternalFunction("Console.println", ...) 调用。
//****************************************************************************

using System;
using SimpleLanguage.VM.Runtime;

namespace StdNativeImpl
{
    /// <summary>
    /// Console 外部函数模块实现。
    /// VM 加载此 DLL 时会自动发现并调用 Register 方法。
    /// </summary>
    public class ConsoleExternalModule : ISLExternalFunctionModule
    {
        public void Register(ISLExternalFunctionRegistrar registrar)
        {
            // Console.println(string text, params object[] param)
            registrar.Register("Console.println", (args) =>
            {
                if (args.Length == 0)
                {
                    Console.WriteLine();
                    return null;
                }
                string text = args[0]?.ToString() ?? "";
                if (args.Length > 1 && args[1] is object[] paramArr && paramArr.Length > 0)
                {
                    text = string.Format(text, paramArr);
                }
                Console.WriteLine(text);
                return null;
            });

            // Console.print(string text, params object[] param)
            registrar.Register("Console.print", (args) =>
            {
                if (args.Length == 0)
                {
                    Console.Write("");
                    return null;
                }
                string text = args[0]?.ToString() ?? "";
                if (args.Length > 1 && args[1] is object[] paramArr && paramArr.Length > 0)
                {
                    text = string.Format(text, paramArr);
                }
                Console.Write(text);
                return null;
            });

            // Console.write (alias for print)
            registrar.Register("Console.write", (args) =>
            {
                if (args.Length == 0)
                {
                    Console.Write("");
                    return null;
                }
                string text = args[0]?.ToString() ?? "";
                if (args.Length > 1 && args[1] is object[] paramArr && paramArr.Length > 0)
                {
                    text = string.Format(text, paramArr);
                }
                Console.Write(text);
                return null;
            });

            // Console.input() -> string
            registrar.Register("Console.input", (args) =>
            {
                return Console.ReadLine() ?? "";
            });

            // Console.readLine() -> string
            registrar.Register("Console.readLine", (args) =>
            {
                return Console.ReadLine() ?? "";
            });

            // Console.readKey() -> string
            registrar.Register("Console.readKey", (args) =>
            {
                var key = Console.ReadKey(intercept: true);
                return key.KeyChar.ToString();
            });
        }
    }
}
