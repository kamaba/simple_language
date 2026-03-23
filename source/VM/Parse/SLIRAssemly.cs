using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleLanguage.Parse;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// Runtime view of one loaded package graph node.
    /// Multi-module JSON is flattened into <see cref="moduleList"/>.
    /// </summary>
    public sealed class SLAssembly
    {
        public string id { get; }
        public IReadOnlyList<SLModulePackage> moduleList => m_ModuleList;

        private readonly List<SLModulePackage> m_ModuleList = new();

        private static Dictionary<int, string> s_ConstStringDict = new();

        public SLAssembly(string id)
        {
            this.id = id ?? string.Empty;
        }

        internal void AddModule(SLModulePackage m)
        {
            if (m == null) return;
            m_ModuleList.Add(m);
        }

        /// <summary>All <see cref="SLGlobalStaticVariablePackage"/> entries from every module, in module order.</summary>
        public IEnumerable<SLGlobalStaticVariablePackage> EnumerateGlobalStaticVariables()
        {
            foreach (var mod in m_ModuleList)
            {
                if (mod?.globalStaticVariableList == null) continue;
                foreach (var gv in mod.globalStaticVariableList)
                {
                    if (gv != null) yield return gv;
                }
            }
        }

        /// <summary>All <see cref="SLClassPackage"/> entries from every module, in module order.</summary>
        public IEnumerable<SLClassPackage> EnumerateClasses()
        {
            foreach (var mod in m_ModuleList)
            {
                if (mod?.classList == null) continue;
                foreach (var c in mod.classList)
                {
                    if (c != null) yield return c;
                }
            }
        }
        public static void SetConstStringDict(Dictionary<int, string>? dict)
        {
            s_ConstStringDict = dict ?? new Dictionary<int, string>();
        }

        public static string? TryGetConstString(int id)
        {
            if (s_ConstStringDict != null && s_ConstStringDict.TryGetValue(id, out var s)) return s;
            return null;
        }
    }
    public sealed class IRStringItem
    {
        public int id { get; set; }
        public string value { get; set; } = string.Empty;
    }
    public sealed class SLIRModuleParseResult
    {
        public List<SLPackageRootJson> packageList { get; init; } = new();
        public List<SLAssembly> assemblyList { get; init; } = new();
        public SLAssembly? assembly { get; init; }
        public SLPackageRootJson? currentPackage { get; init; }
        public string? entryMethodId { get; init; }
        public int globalVariableCount { get; init; }
        public int globalInitInstructionCount { get; init; }
    }
}
