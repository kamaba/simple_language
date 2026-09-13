using System;
using System.Collections.Concurrent;

namespace SimpleLanguage.Core
{
    // Lightweight registry for runtime-provided external functions.
    // Front code can query this registry at parse time without referencing VM assemblies.
    public static class ExternalFunctionRegistry
    {
        private static readonly ConcurrentDictionary<string, Delegate> s_map = new ConcurrentDictionary<string, Delegate>(StringComparer.Ordinal);

        public static void Register(string name, Delegate fn)
        {
            if (string.IsNullOrEmpty(name) || fn == null) return;
            s_map[name] = fn;
        }

        public static bool TryGet(string name, out Delegate fn)
        {
            return s_map.TryGetValue(name, out fn);
        }

        public static bool Unregister(string name)
        {
            return s_map.TryRemove(name, out _);
        }
    }
}
