using System;
using System.Collections.Generic;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public sealed class SLAssembly
    {
        public string id { get; }
        public IReadOnlyList<SLModule> moduleList => m_ModuleList;

        private readonly List<SLModule> m_ModuleList = new();

        public SLAssembly(string id)
        {
            this.id = id ?? string.Empty;
        }

        internal void AddModule(SLModule m)
        {
            if (m == null) return;
            m_ModuleList.Add(m);
        }
    }

    public sealed class SLModule
    {
        public string name { get; }
        public IReadOnlyList<SLNamespace> namespaceList => m_NamespaceList;

        private readonly List<SLNamespace> m_NamespaceList = new();
        private readonly Dictionary<string, SLNamespace> m_NamespaceMap = new(StringComparer.Ordinal);

        public SLModule(string name)
        {
            this.name = name ?? string.Empty;
        }

        internal SLNamespace GetOrAddNamespace(string fullName)
        {
            fullName ??= string.Empty;
            if (m_NamespaceMap.TryGetValue(fullName, out var existed)) return existed;
            var ns = new SLNamespace(fullName);
            m_NamespaceMap[fullName] = ns;
            m_NamespaceList.Add(ns);
            return ns;
        }
    }

    public sealed class SLNamespace
    {
        public string fullName { get; }
        public IReadOnlyList<SLTypeMeta> typeList => m_TypeList;

        private readonly List<SLTypeMeta> m_TypeList = new();

        public SLNamespace(string fullName)
        {
            this.fullName = fullName ?? string.Empty;
        }

        internal void AddType(SLTypeMeta t)
        {
            if (t == null) return;
            m_TypeList.Add(t);
        }
    }

    public sealed class SLTypeMeta
    {
        public string name { get; init; } = string.Empty;
        public string fullName { get; init; } = string.Empty;

        public IReadOnlyList<SLMethodMeta> methodList => m_MethodList;
        private readonly List<SLMethodMeta> m_MethodList = new();

        internal void AddMethod(SLMethodMeta m)
        {
            if (m == null) return;
            m_MethodList.Add(m);
        }
    }

    public sealed class SLMethodMeta
    {
        public string id { get; init; } = string.Empty;
        public string name { get; init; } = string.Empty;
        public IReadOnlyList<object> irList { get; init; } = Array.Empty<object>();
        public IReadOnlyList<SimpleLanguage.VM.Instruction> vmInstructionList { get; init; } = Array.Empty<SimpleLanguage.VM.Instruction>();
    }
}
