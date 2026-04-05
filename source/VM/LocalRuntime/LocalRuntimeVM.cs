using System;
using System.Collections.Generic;
using SimpleLanguage.VM;

namespace SimpleLanguage.VM.Runtime
{
    // Minimal local runtime VM bridge that allows the VM to call
    // static library helpers (NumClass, ByteClass, etc.) by name.
    //
    // Design:
    // - Singleton `LocalRuntimeVM.Instance` to be used from VM code.
    // - Register library functions into a name -> Delegate map.
    // - Provide `Invoke` / `TryInvoke` helpers that DynamicInvoke the delegate.
    public class LocalRuntimeVM
    {
        public static LocalRuntimeVM Instance { get; } = new LocalRuntimeVM();

        private readonly Dictionary<string, Delegate> m_Functions = new Dictionary<string, Delegate>(StringComparer.Ordinal);

        private LocalRuntimeVM()
        {
            // do not auto-register to avoid creating direct dependency to Front registry here
            // caller (VM startup) should call RegisterBuiltinsAndBridge() to also register into Front's registry
        }

        public void RegisterBuiltinsAndBridge()
        {
            // register local implementations
            //RegisterBuiltins();
            // also register into front-side ExternalFunctionRegistry so front can discover them without referencing VM
            try
            {
                // locate the ExternalFunctionRegistry type via reflection from any loaded assembly
                Type registryType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var t = asm.GetType("SimpleLanguage.Core.ExternalFunctionRegistry");
                        if (t != null) { registryType = t; break; }
                    }
                    catch { }
                }
                var mi = registryType?.GetMethod("Register", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (mi != null)
                {
                    foreach (var kv in new string[] { "Num.ToInt32", "Num.ToInt64", "Num.ToFloat64", "Num.ToFloat32", "Num.ToBool", "Byte.ParseInt", "Byte.ToRadixString" })
                    {
                        if (m_Functions.TryGetValue(kv, out var del))
                        {
                            try { mi.Invoke(null, new object[] { kv, del }); } catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public void Register(string name, Delegate del)
        {
            if (string.IsNullOrEmpty(name) || del == null) return;
            m_Functions[name] = del;
        }

        public bool TryGet(string name, out Delegate del)
        {
            return m_Functions.TryGetValue(name, out del);
        }

        public object Invoke(string name, params object[] args)
        {
            if (!m_Functions.TryGetValue(name, out var del))
                throw new InvalidOperationException($"Function not registered: {name}");
            return del.DynamicInvoke(args);
        }

        public bool TryInvoke(string name, out object result, params object[] args)
        {
            result = null;
            if (!m_Functions.TryGetValue(name, out var del)) return false;
            try
            {
                result = del.DynamicInvoke(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //private void RegisterBuiltins()
        //{
        //    // Numeric helpers (use library implementations)
        //    Register("Num.ToInt32", new Func<object, int>(SimpleLanguage.Lib.NumClass.NumToInt32));
        //    Register("Num.ToInt64", new Func<object, long>(SimpleLanguage.Lib.NumClass.NumToInt64));
        //    Register("Num.ToFloat64", new Func<object, double>(SimpleLanguage.Lib.NumClass.NumToFloat64));
        //    Register("Num.ToFloat32", new Func<object, float>(SimpleLanguage.Lib.NumClass.NumToFloat32));
        //    Register("Num.ToBool", new Func<object, bool>(SimpleLanguage.Lib.NumClass.NumToBool));
        //}
    }
}
