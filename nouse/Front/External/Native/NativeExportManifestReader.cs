using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SimpleLanguage.External.Native
{
    public static class NativeExportManifestReader
    {
        // Convention: native library exports a function:
        //   const char* sl_exports_json();
        // returning a UTF-8 JSON string.
        private const string ExportFunctionName = "sl_exports_json";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nint SlExportsJson();

        private static readonly NativeLibraryLoader s_loader = new NativeLibraryLoader();

        public static SLNativeLibraryExportManifest ReadFromJsonFile(string manifestJsonPath, string libraryPath = null)
        {
            if (string.IsNullOrWhiteSpace(manifestJsonPath)) throw new ArgumentException(nameof(manifestJsonPath));
            var json = File.ReadAllText(manifestJsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var manifest = JsonSerializer.Deserialize<SLNativeLibraryExportManifest>(json, options);
            if (manifest == null) throw new InvalidOperationException("Failed to parse manifest JSON");
            if (!string.IsNullOrWhiteSpace(libraryPath))
                manifest.libraryPath = Path.GetFullPath(libraryPath);
            return manifest;
        }

        public static SLNativeLibraryExportManifest ReadFromLibrary(string libraryPath)
        {
            if (string.IsNullOrWhiteSpace(libraryPath)) throw new ArgumentException(nameof(libraryPath));
            libraryPath = Path.GetFullPath(libraryPath);

            SLNativeLibraryExportManifest manifest = null;
            try
            {
                var h = s_loader.Load(libraryPath);
                var p = s_loader.GetExport(h, ExportFunctionName);
                var fn = Marshal.GetDelegateForFunctionPointer<SlExportsJson>(p);

                var strPtr = fn();
                if (strPtr == nint.Zero) throw new InvalidOperationException($"{ExportFunctionName} returned null");

                var json = Marshal.PtrToStringUTF8(strPtr);
                if (string.IsNullOrWhiteSpace(json)) throw new InvalidOperationException("Manifest JSON is empty");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                manifest = JsonSerializer.Deserialize<SLNativeLibraryExportManifest>(json, options);
            }
            catch
            {
                // ignored - fall back to file-based manifest
            }

            if (manifest == null)
            {
                var sidecar = libraryPath + ".slffi.json";
                if (!File.Exists(sidecar))
                    throw new FileNotFoundException($"Native export manifest not found. Provide {ExportFunctionName} export or sidecar file: {sidecar}", sidecar);

                manifest = ReadFromJsonFile(sidecar, libraryPath);
            }

            if (manifest == null) throw new InvalidOperationException("Failed to load manifest");

            if (string.IsNullOrWhiteSpace(manifest.libraryPath))
                manifest.libraryPath = libraryPath;

            return manifest;
        }
    }
}
