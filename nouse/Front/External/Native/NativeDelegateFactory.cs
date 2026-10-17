using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace SimpleLanguage.External.Native
{
    public static class NativeDelegateFactory
    {
        public static Delegate CreateDelegate(nint functionPtr, SLNativeCallingConvention callingConvention, SLNativeValueType returnType, IReadOnlyList<SLNativeValueType> parameterTypes)
        {
            if (functionPtr == nint.Zero) throw new ArgumentException(nameof(functionPtr));
            parameterTypes ??= Array.Empty<SLNativeValueType>();

            var paramClrTypes = parameterTypes.Select(MapClrType).ToArray();
            var retClrType = MapClrType(returnType);

            var delegateType = Expression.GetDelegateType(paramClrTypes.Concat(new[] { retClrType }).ToArray());
            return Marshal.GetDelegateForFunctionPointer(functionPtr, delegateType);
        }

        private static Type MapClrType(SLNativeValueType vt)
        {
            return vt switch
            {
                SLNativeValueType.Void => typeof(void),
                SLNativeValueType.Bool => typeof(bool),
                SLNativeValueType.I32 => typeof(int),
                SLNativeValueType.I64 => typeof(long),
                SLNativeValueType.F32 => typeof(float),
                SLNativeValueType.F64 => typeof(double),
                SLNativeValueType.Ptr => typeof(nint),
                SLNativeValueType.Utf8String => typeof(nint),
                _ => typeof(nint),
            };
        }

        public static object[] MarshalArgs(IReadOnlyList<SLNativeValueType> paramTypes, object[] args)
        {
            if (paramTypes == null || paramTypes.Count == 0) return args ?? Array.Empty<object>();
            if (args == null) return Array.Empty<object>();

            var outArgs = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                var vt = i < paramTypes.Count ? paramTypes[i] : SLNativeValueType.Ptr;
                outArgs[i] = MarshalArg(vt, args[i]);
            }
            return outArgs;
        }

        private static object MarshalArg(SLNativeValueType vt, object v)
        {
            if (v == null) return vt == SLNativeValueType.Utf8String ? nint.Zero : v;

            return vt switch
            {
                SLNativeValueType.Utf8String => v is string s ? Marshal.StringToCoTaskMemUTF8(s) : v,
                _ => v,
            };
        }

        public static object MarshalReturn(SLNativeValueType retType, object ret)
        {
            if (retType != SLNativeValueType.Utf8String) return ret;

            if (ret is nint p && p != nint.Zero)
                return Marshal.PtrToStringUTF8(p);

            return null;
        }

        public static void CleanupArgs(IReadOnlyList<SLNativeValueType> paramTypes, object[] marshaledArgs)
        {
            if (paramTypes == null || marshaledArgs == null) return;

            for (int i = 0; i < marshaledArgs.Length && i < paramTypes.Count; i++)
            {
                if (paramTypes[i] == SLNativeValueType.Utf8String && marshaledArgs[i] is nint p && p != nint.Zero)
                {
                    Marshal.FreeCoTaskMem(p);
                }
            }
        }
    }
}
