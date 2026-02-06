using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleLanguage.VM.InnerCLRRuntime.IL;

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
        public IReadOnlyList<ILInstruction> ilInstructionList { get; init; } = Array.Empty<ILInstruction>();
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

        public RuntimeModule BuildMeta(RuntimeModule rm, Func<Type, bool> typeFilter = null)
        {
            if (rm == null) throw new ArgumentNullException(nameof(rm));

            Type[] types;
            try
            {
                types = rm.module.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                types = rtle.Types.Where(t => t != null).ToArray();
                foreach (var le in rtle.LoaderExceptions)
                {
                    if (le != null) Log.AddVM(EError.None, $"Module type load error: {le.Message}");
                }
            }

            typeFilter ??= (_ => true);

            foreach (var t in types)
            {
                if (t == null) continue;
                if (!typeFilter(t)) continue;
                if (t.IsNested) continue;

                var nsName = t.Namespace ?? string.Empty;
                var ns = rm.GetOrAddNamespace(nsName);

                var tm = new RuntimeTypeMeta
                {
                    namespaceName = nsName,
                    name = t.Name,
                    fullName = t.FullName ?? t.Name,
                };

                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (m == null) continue;
                    if (m.IsSpecialName) continue;

                    var body = SafeGetMethodBody(m);
                    var il = body?.GetILAsByteArray();
                    var ilIns = (il != null && il.Length > 0) ? ILReader.Read(rm.module, il) : new List<ILInstruction>();

                    var mm = new RuntimeMethodMeta
                    {
                        name = m.Name,
                        returnType = m.ReturnType?.FullName ?? m.ReturnType?.Name ?? string.Empty,
                        parameterTypeList = m.GetParameters().Select(p => p.ParameterType?.FullName ?? p.ParameterType?.Name ?? string.Empty).ToArray(),
                        isStatic = m.IsStatic,
                        isPublic = m.IsPublic,
                        ilBytes = il,
                        ilInstructionList = ilIns,
                    };
                    tm.AddMethod(mm);
                }

                ns.AddType(tm);
            }

            return rm;
        }

        private static Module ChooseModule(Assembly asm, string moduleName)
        {
            if (asm == null) return null;
            if (string.IsNullOrWhiteSpace(moduleName)) return asm.ManifestModule;

            var modules = asm.GetModules();
            return modules.FirstOrDefault(m => string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                   ?? asm.ManifestModule;
        }

        private static MethodBody SafeGetMethodBody(MethodInfo mi)
        {
            try
            {
                return mi.GetMethodBody();
            }
            catch
            {
                return null;
            }
        }
    }
}
