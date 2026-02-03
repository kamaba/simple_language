using System;
using System.Collections.Concurrent;
using SimpleLanguage.Core;

namespace SimpleLanguage.Core
{
    // Registry for externally provided CSharp classes or namespaces.
    // Front code can register MetaNode instances (MetaClass/MetaNamespace)
    // so that `CSharp.` lookups can find them without scanning assemblies.
    public static class ExternalClassRegistry
    {
        private static readonly ConcurrentDictionary<string, MetaNode> s_map = new ConcurrentDictionary<string, MetaNode>(StringComparer.Ordinal);

        // Register a MetaNode for a full name like "System.Text.StringBuilder" or a namespace
        public static void Register(string fullName, MetaNode node)
        {
            if (string.IsNullOrEmpty(fullName) || node == null) return;
            s_map[fullName] = node;
        }

        public static bool TryGet(string fullName, out MetaNode node)
        {
            return s_map.TryGetValue(fullName, out node);
        }

        public static bool Unregister(string fullName)
        {
            return s_map.TryRemove(fullName, out _);
        }
    }
}
