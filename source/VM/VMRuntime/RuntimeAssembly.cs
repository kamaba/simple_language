using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SimpleLanguage.Logging;

namespace SimpleLanguage.VM
{
    public sealed class RuntimeAssembly
    {
        public string id { get; }
        public Assembly assembly { get; }
        public IReadOnlyList<RuntimeModule> moduleList => m_ModuleList;

        private readonly List<RuntimeModule> m_ModuleList = new();

        public RuntimeAssembly(string id, Assembly asm)
        {
            this.id = id ?? string.Empty;
            assembly = asm ?? throw new ArgumentNullException(nameof(asm));
        }

        internal void AddModule(RuntimeModule rm)
        {
            if (rm == null) return;
            m_ModuleList.Add(rm);
        }
    }

    public sealed class RuntimeAssemblyManager
    {
        private readonly Dictionary<string, RuntimeAssembly> m_AssemblyMap = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string id, out RuntimeAssembly ra) => m_AssemblyMap.TryGetValue(id, out ra);

        public RuntimeAssembly LoadFromPath(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath)) throw new ArgumentException(nameof(assemblyPath));
            assemblyPath = Path.GetFullPath(assemblyPath);

            if (!File.Exists(assemblyPath))
            {
                Log.AddVM(LID.Unknown, $"LoadFromPath: assembly not found: {assemblyPath}");
                return null;
            }

            var asm = Assembly.LoadFrom(assemblyPath);
            var id = asm.GetName().Name ?? Path.GetFileNameWithoutExtension(assemblyPath);
            if (m_AssemblyMap.TryGetValue(id, out var existed)) return existed;

            var ra = new RuntimeAssembly(id, asm);
            m_AssemblyMap[id] = ra;
            return ra;
        }

        public RuntimeAssembly Load(Assembly asm)
        {
            if (asm == null) throw new ArgumentNullException(nameof(asm));
            var id = asm.GetName().Name ?? asm.FullName ?? string.Empty;
            if (m_AssemblyMap.TryGetValue(id, out var existed)) return existed;
            var ra = new RuntimeAssembly(id, asm);
            m_AssemblyMap[id] = ra;
            return ra;
        }

        public IReadOnlyList<RuntimeAssembly> GetAll() => m_AssemblyMap.Values.ToList();
    }
}
