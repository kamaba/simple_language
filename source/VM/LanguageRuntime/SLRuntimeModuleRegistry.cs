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

                s_MethodById[rm.id] = rm;
            }
        }

        public static RuntimeMethod? GetMethod(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return s_MethodById.TryGetValue(id, out var m) ? m : null;
        }
    }
}
