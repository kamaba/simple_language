//****************************************************************************
//  MathNativeImpl: Math 库的原生实现。
//  VM 加载 Math 模块时自动加载此 DLL，注册的函数可被 .sl 中的
//  SystemCallExternalFunction("Math.sin", ...) 调用。
//****************************************************************************

using System;
using SimpleLanguage.VM.Runtime;

namespace MathNativeImpl
{
    public class MathExternalModule : ISLExternalFunctionModule
    {
        public void Register(ISLExternalFunctionRegistrar registrar)
        {
            // Trigonometric
            RegisterFloat(registrar, "Math.sin", v => (float)Math.Sin(v));
            RegisterFloat(registrar, "Math.cos", v => (float)Math.Cos(v));
            RegisterFloat(registrar, "Math.tan", v => (float)Math.Tan(v));
            RegisterFloat(registrar, "Math.asin", v => (float)Math.Asin(v));
            RegisterFloat(registrar, "Math.acos", v => (float)Math.Acos(v));
            RegisterFloat(registrar, "Math.atan", v => (float)Math.Atan(v));
            RegisterFloat2(registrar, "Math.atan2", (y, x) => (float)Math.Atan2(y, x));

            // Hyperbolic
            RegisterFloat(registrar, "Math.sinh", v => (float)Math.Sinh(v));
            RegisterFloat(registrar, "Math.cosh", v => (float)Math.Cosh(v));
            RegisterFloat(registrar, "Math.tanh", v => (float)Math.Tanh(v));

            // Power / Log
            RegisterFloat2(registrar, "Math.pow", (b, e) => (float)Math.Pow(b, e));
            RegisterFloat(registrar, "Math.sqrt", v => (float)Math.Sqrt(v));
            RegisterFloat(registrar, "Math.exp", v => (float)Math.Exp(v));
            RegisterFloat(registrar, "Math.log", v => (float)Math.Log(v));
            RegisterFloat(registrar, "Math.log10", v => (float)Math.Log10(v));

            // Rounding
            RegisterFloat(registrar, "Math.ceil", v => (float)Math.Ceiling(v));
            RegisterFloat(registrar, "Math.floor", v => (float)Math.Floor(v));
            RegisterFloat(registrar, "Math.round", v => (float)Math.Round(v));
            RegisterIntFromFloat(registrar, "Math.truncate", v => (int)Math.Truncate(v));
        }

        private static float ReadFloat(object?[] args, int index = 0)
        {
            if (index >= args.Length || args[index] == null) return 0f;
            return args[index] switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                long l => l,
                string s => float.TryParse(s, out var v) ? v : 0f,
                _ => Convert.ToSingle(args[index]),
            };
        }

        private static void RegisterFloat(ISLExternalFunctionRegistrar r, string name, Func<float, float> fn)
        {
            r.Register(name, args => fn(ReadFloat(args)));
        }

        private static void RegisterFloat2(ISLExternalFunctionRegistrar r, string name, Func<float, float, float> fn)
        {
            r.Register(name, args => fn(ReadFloat(args, 0), ReadFloat(args, 1)));
        }

        private static void RegisterIntFromFloat(ISLExternalFunctionRegistrar r, string name, Func<float, int> fn)
        {
            r.Register(name, args => fn(ReadFloat(args)));
        }
    }
}
