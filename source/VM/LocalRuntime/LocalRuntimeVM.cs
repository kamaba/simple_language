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
            RegisterBuiltins();
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

        private void RegisterBuiltins()
        {
            // Numeric helpers
            //Register("Num.ToInt32", new Func<object, int>(NumClass.NumToInt32));
            //Register("Num.ToInt64", new Func<object, long>(NumClass.NumToInt64));
            //Register("Num.ToFloat64", new Func<object, double>(NumClass.NumToFloat64));
            //Register("Num.ToFloat32", new Func<object, float>(NumClass.NumToFloat32));
            //Register("Num.ToBool", new Func<object, bool>(NumClass.NumToBool));

            //// Byte helpers
            //Register("Byte.ParseInt", new Func<string, int>(ByteClass.Parse));
            //Register("Byte.ToRadixString", new Func<int, int, string>(ByteClass.ToRadixString));
        }
    }
}
