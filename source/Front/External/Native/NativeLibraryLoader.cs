using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace SimpleLanguage.External.Native
{
    public sealed class NativeLibraryLoader
    {
        private readonly ConcurrentDictionary<string, nint> m_HandleMap = new(StringComparer.OrdinalIgnoreCase);

        public nint Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));

            var full = Path.GetFullPath(path);
            if (m_HandleMap.TryGetValue(full, out var existed)) return existed;

            var handle = NativeLibrary.Load(full);
            m_HandleMap[full] = handle;
            return handle;
        }

        public nint GetExport(nint handle, string entryPoint)
        {
            if (handle == nint.Zero) throw new ArgumentException(nameof(handle));
            if (string.IsNullOrWhiteSpace(entryPoint)) throw new ArgumentException(nameof(entryPoint));
            return NativeLibrary.GetExport(handle, entryPoint);
        }

        public bool TryFree(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var full = Path.GetFullPath(path);
            if (!m_HandleMap.TryRemove(full, out var h)) return false;
            if (h == nint.Zero) return false;
            NativeLibrary.Free(h);
            return true;
        }
    }
}
