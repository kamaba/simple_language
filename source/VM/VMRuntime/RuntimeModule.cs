using System.Reflection;

namespace SimpleLanguage.VM
{
    public class RuntimeModule
    {
        public string id { get; }
        public RuntimeAssembly ownerAssembly { get; }
        public Module module { get; }

        public IReadOnlyList<RuntimeNamespace> namespaceList => m_NamespaceList;

        private readonly List<RuntimeNamespace> m_NamespaceList = new();
        private readonly Dictionary<string, RuntimeNamespace> m_NamespaceMap = new(StringComparer.Ordinal);

        public RuntimeModule(string id, RuntimeAssembly ownerAssembly, Module module)
        {
            this.id = id ?? string.Empty;
            this.ownerAssembly = ownerAssembly;
            this.module = module ?? throw new ArgumentNullException(nameof(module));
        }

        internal RuntimeNamespace GetOrAddNamespace(string namespaceName)
        {
            namespaceName ??= string.Empty;
            if (m_NamespaceMap.TryGetValue(namespaceName, out var existed)) return existed;
            var ns = new RuntimeNamespace(namespaceName);
            m_NamespaceMap[namespaceName] = ns;
            m_NamespaceList.Add(ns);
            return ns;
        }
    }

    public sealed class RuntimeNamespace
    {
        public string name { get; }
        public IReadOnlyList<RuntimeTypeMeta> typeList => m_TypeList;

        private readonly List<RuntimeTypeMeta> m_TypeList = new();

        public RuntimeNamespace(string name)
        {
            this.name = name ?? string.Empty;
        }

        internal void AddType(RuntimeTypeMeta t)
        {
            if (t == null) return;
            m_TypeList.Add(t);
        }
    }

    public sealed class RuntimeTypeMeta
    {
        public string namespaceName { get; init; } = string.Empty;
        public string name { get; init; } = string.Empty;
        public string fullName { get; init; } = string.Empty;

        public IReadOnlyList<RuntimeMethodMeta> methodList => m_MethodList;
        private readonly List<RuntimeMethodMeta> m_MethodList = new();

        internal void AddMethod(RuntimeMethodMeta m)
        {
            if (m == null) return;
            m_MethodList.Add(m);
        }
    }

    public sealed class RuntimeMethodMeta
    {
        public string name { get; init; } = string.Empty;
        public string returnType { get; init; } = string.Empty;
        public IReadOnlyList<string> parameterTypeList { get; init; } = Array.Empty<string>();
        public bool isStatic { get; init; }
        public bool isPublic { get; init; }
        public byte[] ilBytes { get; init; }
        public IReadOnlyList<Instruction> ilInstructionList { get; init; } = Array.Empty<Instruction>();
    }

    public class RuntimeModuleManager
    {
        private readonly Dictionary<string, RuntimeModule> m_ModuleMap = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string id, out RuntimeModule m) => m_ModuleMap.TryGetValue(id, out m);

        public RuntimeModule LoadFromAssembly(RuntimeAssembly ra, string moduleName = null)
        {
            if (ra == null) throw new ArgumentNullException(nameof(ra));

            var mod = ChooseModule(ra.assembly, moduleName);
            if (mod == null) return null;

            var id = !string.IsNullOrWhiteSpace(moduleName) ? moduleName : (mod.Name ?? "<module>");
            if (m_ModuleMap.TryGetValue(id, out var existed)) return existed;

            var rm = new RuntimeModule(id, ra, mod);
            m_ModuleMap[id] = rm;
            ra.AddModule(rm);
            return rm;
        }

        public RuntimeModule BuildMetaFromCLR(RuntimeModule rm)
        {
            throw new NotSupportedException("CLR reflection Type is not used for SimpleLanguage. Use SLModulePackageLoader to import SimpleLanguage module metadata into VM.");
        }

        // Build metadata from SimpleLanguage internal type system (not CLR reflection).
        // Front-end should export module/type/method graph into SLModulePackage and VM imports it.
        public RuntimeModule BuildMetaFromSimpleLanguage(RuntimeModule rm)
        {
            throw new NotSupportedException("Use SLModulePackageLoader to import SimpleLanguage module metadata into VM.");
        }

        private static Module ChooseModule(Assembly asm, string moduleName)
        {
            if (asm == null) return null;
            if (string.IsNullOrWhiteSpace(moduleName)) return asm.ManifestModule;

            var modules = asm.GetModules();
            return modules.FirstOrDefault(m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                   ?? asm.ManifestModule;
        }

        private static MethodBody SafeGetMethodBody(MethodInfo mi) => mi?.GetMethodBody();
    }
}
