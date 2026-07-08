using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SimpleLanguage.External.Native
{
    public enum SLNativeCallingConvention
    {
        Cdecl,
        StdCall,
    }

    public enum SLNativeValueType
    {
        Void,
        Bool,
        I32,
        I64,
        F32,
        F64,
        Ptr,
        Utf8String,
    }

    public sealed class SLNativeFunctionExport
    {
        public string publicName { get; set; } = string.Empty;
        public string entryPoint { get; set; } = string.Empty;
        public SLNativeCallingConvention callingConvention { get; set; } = SLNativeCallingConvention.Cdecl;
        public SLNativeValueType returnType { get; set; } = SLNativeValueType.Void;
        public List<SLNativeValueType> parameterTypeList { get; set; } = new();
    }

    public sealed class SLNativeLibraryExportManifest
    {
        public string libraryPath { get; set; } = string.Empty;
        public string baseNamespace { get; set; } = "Native";
        public List<SLNativeFunctionExport> functionList { get; set; } = new();
    }

    public static class SLNativeMarshalling
    {
        public static CallingConvention ToCallingConvention(this SLNativeCallingConvention cc)
            => cc == SLNativeCallingConvention.StdCall ? CallingConvention.StdCall : CallingConvention.Cdecl;
    }
}
