//****************************************************************************
//  File:      RuntimeVM.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SimpleLanguage.VM.Runtime
{
    class Signature
    {
        public string CallConv;
        public List<string> Params = new();
        public string Ret;
    }

    static class SignatureParser
    {
        public static Signature Parse(string sig)
        {
            string cc = "cdecl";

            if (sig.Contains(":"))
            {
                var parts = sig.Split(':');
                cc = parts[0];
                sig = parts[1];
            }

            var body = sig.Split("->");
            var paramPart = body[0].Trim('(', ')');
            var ret = body[1];

            Signature s = new();
            s.CallConv = cc;
            s.Ret = ret;

            if (!string.IsNullOrEmpty(paramPart))
                s.Params.AddRange(paramPart.Split(','));

            return s;
        }
    }

    static class DelegateFactory
    {
        public static Type Build(Signature sig)
        {
            var paramTypes = sig.Params
                .Select(ToClrType)
                .ToList();

            Type retType = ToClrType(sig.Ret);

            //var asm = AssemblyBuilderHelper.DynamicAsm;
            //return asm.BuildDelegateType(
            //    sig.CallConv,
            //    paramTypes.ToArray(),
            //    retType);
            return null;
        }

        static Type ToClrType(string t)
        {
            return t switch
            {
                "int32" => typeof(int),
                "int64" => typeof(long),
                "float32" => typeof(float),
                "float64" => typeof(double),
                "bool" => typeof(bool),
                "void" => typeof(void),
                "string" => typeof(IntPtr),
                _ => throw new Exception("Unknown type " + t)
            };
        }
    }

    static class AssemblyBuilderHelper
    {
        static AssemblyBuilder asm =
            AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("NativeDelegates"),
                AssemblyBuilderAccess.Run);

        static ModuleBuilder mod = asm.DefineDynamicModule("M");

        public static Type BuildDelegateType(
            string callConv,
            Type[] args,
            Type ret)
        {
            string name = "D" + Guid.NewGuid().ToString("N");

            var tb = mod.DefineType(
                name,
                TypeAttributes.Public |
                TypeAttributes.Sealed |
                TypeAttributes.Class,
                typeof(MulticastDelegate));

            var ctor = tb.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(object), typeof(IntPtr) });

            ctor.SetImplementationFlags(
                MethodImplAttributes.Runtime);

            var invoke = tb.DefineMethod(
                "Invoke",
                MethodAttributes.Public |
                MethodAttributes.Virtual,
                ret,
                args);

            invoke.SetImplementationFlags(
                MethodImplAttributes.Runtime);

            var attrCtor =
                typeof(UnmanagedFunctionPointerAttribute)
                .GetConstructor(new[] { typeof(CallingConvention) });

            var cc = callConv == "stdcall"
                ? CallingConvention.StdCall
                : CallingConvention.Cdecl;

            invoke.SetCustomAttribute(
                new CustomAttributeBuilder(
                    attrCtor,
                    new object[] { cc }));

            return tb.CreateType();
        }
    }
    public static class NativeBridge
    {
        static Dictionary<string, Delegate> cache = new();

        public static object Call(
            string dll,
            string func,
            string signature,
            object[] args)
        {
            string key = dll + "|" + func + "|" + signature;

            if (!cache.TryGetValue(key, out var del))
            {
                var sig = SignatureParser.Parse(signature);
                //IntPtr lib = NativeLoader.Load(dll);
                //IntPtr fn = NativeLoader.GetSymbol(lib, func);

                //Type delType = DelegateFactory.Build(sig);
                //del = Marshal.GetDelegateForFunctionPointer(fn, delType);

                //cache[key] = del;
            }

            return del.DynamicInvoke(args);
        }
    }
}
