using System;
using System.Collections.Generic;
using SimpleLanguage.Core;

namespace SimpleLanguage.External.Native
{
    public static class NativeBindingManager
    {
        public static void RegisterFromManifest(SLNativeLibraryExportManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            var loader = new NativeLibraryLoader();
            var h = loader.Load(manifest.libraryPath);

            foreach (var f in manifest.functionList)
            {
                if (f == null) continue;
                if (string.IsNullOrWhiteSpace(f.publicName) || string.IsNullOrWhiteSpace(f.entryPoint)) continue;

                var ptr = loader.GetExport(h, f.entryPoint);
                var d = NativeDelegateFactory.CreateDelegate(ptr, f.callingConvention, f.returnType, f.parameterTypeList);

                // Wrap into a callable delegate that also handles simple marshaling.
                Func<object[], object> wrapper = (args) =>
                {
                    var marshaled = NativeDelegateFactory.MarshalArgs(f.parameterTypeList, args ?? Array.Empty<object>());
                    try
                    {
                        var ret = d.DynamicInvoke(marshaled);
                        return NativeDelegateFactory.MarshalReturn(f.returnType, ret);
                    }
                    finally
                    {
                        NativeDelegateFactory.CleanupArgs(f.parameterTypeList, marshaled);
                    }
                };

                ExternalFunctionRegistry.Register(f.publicName, wrapper);
            }
        }

        public static void RegisterFromLibrary(string libraryPath)
        {
            var manifest = NativeExportManifestReader.ReadFromLibrary(libraryPath);
            RegisterFromManifest(manifest);
        }
    }
}
