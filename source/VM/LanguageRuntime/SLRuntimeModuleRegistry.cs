using System;
using System.Collections.Generic;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public static class SLRuntimeModuleRegistry
    {
        private static readonly Dictionary<string, RuntimeMethod> s_MethodById = new(StringComparer.Ordinal);

        public static void Clear()
        {
            s_MethodById.Clear();
        }

        public static void LoadFromPackage(SLModulePackage pkg)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));
            Clear();
            AddFromPackage(pkg);
        }

        public static void LoadFromPackages(IEnumerable<SLModulePackage> packages)
        {
            if (packages == null) throw new ArgumentNullException(nameof(packages));
            Clear();

            foreach (var pkg in packages)
            {
                if (pkg == null) continue;
                AddFromPackage(pkg);
            }
        }

        private static void AddFromPackage(SLModulePackage pkg)
        {
            foreach (var m in pkg.methodList)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;

                var rm = new RuntimeMethod
                {
                    id = m.id,
                    onlyFunctionName = m.name ?? string.Empty,
                };

                // instructions
                if (m.instructionList != null)
                {
                    rm.InstructionList.AddRange(SLModulePackageLoader.ConvertToVMInstructionList(m.instructionList));
                }

                if (m.returnList != null)
                {
                    foreach (var v in m.returnList)
                    {
                        if (v == null) continue;
                        rm.methodReturnVariableList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }
                if (m.argumentList != null)
                {
                    foreach (var v in m.argumentList)
                    {
                        if (v == null) continue;
                        rm.methodArgumentList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }
                if (m.localList != null)
                {
                    foreach (var v in m.localList)
                    {
                        if (v == null) continue;
                        rm.methodLocalVariableList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }

                s_MethodById[rm.id] = rm;
            }
        }

        private static RuntimeDefType? ResolveRuntimeDefType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            var rc = RuntimeClassManager.instance.GetRuntimeClassByName(typeName)
                ?? RuntimeClassManager.instance.GetRuntimeClassByName(GetShortName(typeName));

            return rc != null ? new RuntimeDefType(rc) : null;
        }

        private static string GetShortName(string full)
        {
            if (string.IsNullOrEmpty(full)) return string.Empty;
            var idx = full.LastIndexOf('.');
            return idx >= 0 && idx + 1 < full.Length ? full[(idx + 1)..] : full;
        }

        public static RuntimeMethod? GetMethod(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return s_MethodById.TryGetValue(id, out var m) ? m : null;
        }
    }
}
