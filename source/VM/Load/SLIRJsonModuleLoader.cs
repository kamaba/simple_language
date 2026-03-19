using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using SimpleLanguage.VM.LanguageRuntime;

namespace SimpleLanguage.VM
{
    public static class SLIRJsonModuleLoader
    {
        public static string? ResolveJsonPath(string[] args)
        {
            if (args != null && args.Length > 0 && args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return args[0];
            }

            var defaultPath = GetDefaultJsonPath();
            return File.Exists(defaultPath) ? defaultPath : null;
        }

        public sealed class Module
        {
            public List<IRStringItem> irStringDict { get; set; } = new();
            public List<ClassModel> classes { get; set; } = new();
            public List<MethodModel> methods { get; set; } = new();
        }

        public sealed class IRStringItem
        {
            public int id { get; set; }
            public string value { get; set; } = string.Empty;
        }

        public sealed class ClassModel
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public string sourcePath { get; set; } = string.Empty;
            public List<FieldModel> fields { get; set; } = new();
        }

        public sealed class FieldModel
        {
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public bool isStatic { get; set; }
            public int index { get; set; }
        }

        public sealed class MethodModel
        {
            public string id { get; set; } = string.Empty;
            public string onlyName { get; set; } = string.Empty;
            public int ownerClassId { get; set; }
            public int argumentCount { get; set; }
            public int localCount { get; set; }
            public int returnCount { get; set; }
            public List<InstructionModel> instructions { get; set; } = new();
        }

        public sealed class InstructionModel
        {
            public string opCode { get; set; } = string.Empty;
            public int index { get; set; }
            public int offset { get; set; }
            public string? payloadBase64 { get; set; }
        }

        private static Dictionary<int, string> s_LastIRStringDict = new();

        public static string? TryGetConstString(int stringId)
        {
            var fromBootstrap = SLIRJsonModuleLoaderBootstrap.TryGetConstString(stringId);
            if (fromBootstrap != null) return fromBootstrap;

            if (s_LastIRStringDict != null && s_LastIRStringDict.TryGetValue(stringId, out var s))
                return s;
            return null;
        }

        public static Module ReadModule(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = GetDefaultJsonPath();
            }
            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var m = JsonSerializer.Deserialize<Module>(json, options) ?? new Module();

            s_LastIRStringDict = new Dictionary<int, string>();
            foreach (var it in m.irStringDict)
            {
                s_LastIRStringDict[it.id] = it.value ?? string.Empty;
            }
            SLIRJsonModuleLoaderBootstrap.SetConstStringDict(s_LastIRStringDict);
            return m;
        }

        public static SLModulePackage ReadPackage(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = GetDefaultJsonPath();
            }

            if (!jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SLIRJsonModuleLoader.ReadPackage only supports module.package.json");
            }

            var pkg = SLModulePackageLoader.LoadFromJson(jsonPath);
            var dict = new Dictionary<int, string>();
            if (pkg?.irStringDict != null)
            {
                for (int i = 0; i < pkg.irStringDict.Count; i++)
                {
                    var item = pkg.irStringDict[i];
                    if (item == null) continue;
                    dict[item.id] = item.value ?? string.Empty;
                }
            }

            s_LastIRStringDict = dict;
            SLIRJsonModuleLoaderBootstrap.SetConstStringDict(dict);
            return pkg;
        }

        public static string GetDefaultJsonPath()
        {
            var outDir = "E:\\project\\lang\\simple_language\\source\\Front\\bin\\Debug\\net8.0\\out\\export";// Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Environment.CurrentDirectory, "out", "export");
            }

            var packageJson = Path.Combine(outDir, "module.package.json");
            if (File.Exists(packageJson)) return packageJson;

            return Path.Combine(outDir, "module.slir.json");
        }

        public static void LoadIntoRuntime(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = GetDefaultJsonPath();
            }

            // Keep currently running runtime path first: package.json + registry binding.
            if (jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
            {
                var pkg = ReadPackage(jsonPath);

                SLRuntimeModuleRegistry.LoadFromPackage(pkg);
                return;
            }

            var m = ReadModule(jsonPath);

            var rcm = RuntimeClassManager.instance;
            rcm.m_IRMetaClassList.Clear();

            // Create runtime classes
            for (int i = 0; i < m.classes.Count; i++)
            {
                var c = m.classes[i];
                var rc = new RuntimeClass
                {
                    id = StableId32(c.name),
                    name = c.name ?? string.Empty,
                };
                rcm.m_IRMetaClassList.Add(rc);
            }

            for (int i = 0; i < rcm.m_IRMetaClassList.Count; i++)
            {
                var rc = rcm.m_IRMetaClassList[i];
                if (rc == null) continue;
                if (RuntimeTypeManager.GetRuntimeTypeByClassId(rc.id) == null)
                {
                    RuntimeTypeManager.AddRuntimeTypeByClass(rc);
                }
            }

            // Fields: JSON currently stores type as string only; RuntimeDefType cannot be reconstructed without TypeSig.
            // Keep variables but leave RuntimeDefType null for now.
            for (int i = 0; i < m.classes.Count; i++)
            {
                var c = m.classes[i];
                var rc = rcm.GetRuntimeClassByName(c.name);
                if (rc == null) continue;

                foreach (var f in c.fields)
                {
                    var rv = new RuntimeVariable();
                    if (f.isStatic)
                        rc.staticIRMetaVariableList.Add(rv);
                    else
                        rc.localIRMetaVariableList.Add(rv);
                }
            }

            // Methods: consumer can interpret instructions and run via existing VM pipeline once method binding is added.
        }

        private static int StableId32(string s)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }
                return (int)hash;
            }
        }
    }
}
