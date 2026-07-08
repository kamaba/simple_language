using System;
using System.Runtime.InteropServices;

namespace SimpleLanguage.External.Native.DartStyle
{
    public sealed class DartStyleNativeLibrary : IDisposable
    {
        public string path { get; }
        public nint handle { get; private set; }

        public DartStyleNativeLibrary(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            this.path = System.IO.Path.GetFullPath(path);
            handle = NativeLibrary.Load(this.path);
        }

        public nint GetExport(string entryPoint)
        {
            if (string.IsNullOrWhiteSpace(entryPoint)) throw new ArgumentException(nameof(entryPoint));
            if (handle == nint.Zero) throw new ObjectDisposedException(nameof(DartStyleNativeLibrary));
            return NativeLibrary.GetExport(handle, entryPoint);
        }

        public void Dispose()
        {
            if (handle != nint.Zero)
            {
                NativeLibrary.Free(handle);
                handle = nint.Zero;
            }
        }
    }

    public static class DartStyleNative
    {
        public static TDelegate Lookup<TDelegate>(DartStyleNativeLibrary lib, string entryPoint) where TDelegate : Delegate
        {
            if (lib == null) throw new ArgumentNullException(nameof(lib));
            var ptr = lib.GetExport(entryPoint);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
        }

        public static unsafe delegate* unmanaged[Cdecl]<T1, T2, TRet> LookupCdecl<T1, T2, TRet>(DartStyleNativeLibrary lib, string entryPoint)
            where T1 : unmanaged
            where T2 : unmanaged
            where TRet : unmanaged
        {
            if (lib == null) throw new ArgumentNullException(nameof(lib));
            var ptr = lib.GetExport(entryPoint);
            return (delegate* unmanaged[Cdecl]<T1, T2, TRet>)ptr;
        }
    }
}
