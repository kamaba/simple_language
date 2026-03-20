using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using SimpleLanguage.VM.LanguageRuntime;

namespace SimpleLanguage.VM
{
    public static class SLIRJsonModuleLoader
    {
        public sealed class PackageGraph
        {
            public string rootPackagePath { get; init; } = string.Empty;
            public string rootDirectory { get; init; } = string.Empty;
            public List<SLModulePackage> packageList { get; init; } = new();
        }

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

        public sealed class IRStringItem { public int id { get; set; } public string value { get; set; } = string.Empty; }
        public sealed class ClassModel { public int id { get; set; } public string name { get; set; } = string.Empty; public string sourcePath { get; set; } = string.Empty; public List<FieldModel> fields { get; set; } = new(); }
        public sealed class FieldModel { public string name { get; set; } = string.Empty; public string type { get; set; } = string.Empty; public bool isStatic { get; set; } public int index { get; set; } }
        public sealed class MethodModel { public string id { get; set; } = string.Empty; public string onlyName { get; set; } = string.Empty; public int ownerClassId { get; set; } public int argumentCount { get; set; } public int localCount { get; set; } public int returnCount { get; set; } public List<InstructionModel> instructions { get; set; } = new(); }
        public sealed class InstructionModel { public string opCode { get; set; } = string.Empty; public int index { get; set; } public int offset { get; set; } public string? payloadBase64 { get; set; } }

        public static string? TryGetConstString(int stringId) => SLIRModuleParse.TryGetConstString(stringId);

        public static Module ReadModule(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Module>(json, options) ?? new Module();
        }

        // Merged helpers from SLModulePackageLoader
        public static SLModulePackage LoadFromJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());
            var pkg = JsonSerializer.Deserialize<SLModulePackage>(json, options);
            NormalizeFieldFlags(pkg);
            return pkg;
        }

        private static void NormalizeFieldFlags(SLModulePackage? pkg)
        {
            if (pkg?.classList == null) return;
            for (int c = 0; c < pkg.classList.Count; c++)
            {
                var cls = pkg.classList[c];
                if (cls?.fieldList == null) continue;
                for (int f = 0; f < cls.fieldList.Count; f++)
                {
                    var field = cls.fieldList[f];
                    if (field == null) continue;
                    if (field.isConst) field.flags |= 16;
                    if (field.isStatic) field.flags |= 32;
                    if (!field.isConst && (field.flags & 16) == 16) field.isConst = true;
                    if (!field.isStatic && (field.flags & 32) == 32) field.isStatic = true;
                }
            }
        }
        public static SLAssembly BuildRuntimeModel(SLModulePackage pkg)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));
            var asm = new SLAssembly("SimpleLanguage");
            var module = new SLModule(pkg.moduleName);
            asm.AddModule(module);
            foreach (var nsPkg in pkg.namespaceList ?? Enumerable.Empty<SLNamespacePackage>())
            {
                var ns = module.GetOrAddNamespace(nsPkg.fullName);
                foreach (var t in nsPkg.typeList ?? Enumerable.Empty<SLTypePackage>())
                {
                    var full = NormalizeTypeName(t.fullName);
                    ns.AddType(new SLTypeMeta { name = GetTypeShortName(full), fullName = full });
                }
            }
            var typeMap = module.namespaceList
                .SelectMany(n => n.typeList)
                .GroupBy(t => t.fullName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            foreach (var m in pkg.methodList ?? Enumerable.Empty<SLMethodPackage>())
            {
                var declType = NormalizeTypeName(m.declaringTypeFullName);
                if (!typeMap.TryGetValue(declType ?? string.Empty, out var tm))
                {
                    var nsName = GetNamespaceFromFullTypeName(declType);
                    var ns = module.GetOrAddNamespace(nsName);
                    tm = new SLTypeMeta { name = GetTypeShortName(declType), fullName = declType ?? string.Empty };
                    ns.AddType(tm);
                    typeMap[tm.fullName] = tm;
                }
                var vmIns = SLIRModuleParse.ConvertToVMInstructionList(m.instructionList);
                tm.AddMethod(new SLMethodMeta { id = m.id ?? string.Empty, name = m.name ?? string.Empty, irList = Array.Empty<object>(), vmInstructionList = vmIns });
            }
            return asm;
        }

        public static SLModulePackage ReadPackage(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            if (!jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SLIRJsonModuleLoader.ReadPackage only supports module.package.json");
            return LoadFromJson(jsonPath);
        }

        public static PackageGraph ReadPackagesInExecutionOrder(string rootPackagePath)
        {
            if (string.IsNullOrWhiteSpace(rootPackagePath)) throw new ArgumentNullException(nameof(rootPackagePath));
            var rootFullPath = Path.GetFullPath(rootPackagePath);
            var result = new List<SLModulePackage>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void LoadRecursive(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath)) return;
                if (!visited.Add(fullPath)) return;
                var pkg = ReadPackage(fullPath);
                var dir = Path.GetDirectoryName(fullPath) ?? string.Empty;
                var refs = pkg.moduleReferences ?? new List<string>();
                for (int i = 0; i < refs.Count; i++)
                {
                    var rp = refs[i];
                    if (string.IsNullOrWhiteSpace(rp)) continue;
                    var refPath = Path.IsPathRooted(rp) ? rp : Path.Combine(dir, rp);
                    LoadRecursive(refPath);
                }
                result.Add(pkg);
            }
            LoadRecursive(rootFullPath);
            if (result.Count == 1)
            {
                var dir = Path.GetDirectoryName(rootFullPath) ?? string.Empty;
                var siblings = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.package.json") : Array.Empty<string>();
                Array.Sort(siblings, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < siblings.Length; i++)
                {
                    var sp = Path.GetFullPath(siblings[i]);
                    if (string.Equals(sp, rootFullPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!visited.Add(sp)) continue;
                    result.Insert(result.Count - 1, ReadPackage(sp));
                }
            }
            return new PackageGraph { rootPackagePath = rootFullPath, rootDirectory = Path.GetDirectoryName(rootFullPath) ?? string.Empty, packageList = result };
        }

        /*
        public static List<SimpleLanguage.VM.Instruction> ConvertToVMInstructionList(List<SLIRInstructionPackage> list)
        {
            return SLIRJsonModuleLoader.ConvertToVMInstructionList(list);
        }

        public static SLAssembly BuildRuntimeModel(SLModulePackage pkg)
        {
            return SLIRJsonModuleLoader.BuildRuntimeModel(pkg);
        }

        public static void NormalizeFieldFlags(SLModulePackage? pkg)
        {
            // forward: SLIRJsonModuleLoader's NormalizeFieldFlags is private, so nothing to do here
            // Keep as placeholder in case other code calls it.
        }
        */
        public static string GetDefaultJsonPath()
        {
            var outDir = "E:\\project\\lang\\simple_language\\source\\Front\\bin\\Debug\\net8.0\\out\\export"; // Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (string.IsNullOrWhiteSpace(outDir)) outDir = Path.Combine(Environment.CurrentDirectory, "out", "export");
            var packageJson = Path.Combine(outDir, "module.package.json");
            if (File.Exists(packageJson)) return packageJson;
            return Path.Combine(outDir, "module.slir.json");
        }

        public static void LoadIntoRuntime(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            if (jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
            {
                var pkg = ReadPackage(jsonPath);
                SLRuntimeModuleRegistry.LoadFromPackage(pkg);
                return;
            }
            var m = ReadModule(jsonPath);
            var rcm = RuntimeClassManager.instance;
            rcm.m_IRMetaClassList.Clear();
            for (int i = 0; i < m.classes.Count; i++)
            {
                var c = m.classes[i];
                var rc = new RuntimeClass { id = StableId32(c.name), name = c.name ?? string.Empty };
                rcm.m_IRMetaClassList.Add(rc);
            }
            for (int i = 0; i < rcm.m_IRMetaClassList.Count; i++)
            {
                var rc = rcm.m_IRMetaClassList[i];
                if (rc == null) continue;
                if (RuntimeTypeManager.GetRuntimeTypeByClassId(rc.id) == null) RuntimeTypeManager.AddRuntimeTypeByClass(rc);
            }
            for (int i = 0; i < m.classes.Count; i++)
            {
                var c = m.classes[i];
                var rc = rcm.GetRuntimeClassByName(c.name);
                if (rc == null) continue;
                foreach (var f in c.fields)
                {
                    var rv = new RuntimeVariable();
                    if (f.isStatic) rc.staticIRMetaVariableList.Add(rv); else rc.localIRMetaVariableList.Add(rv);
                }
            }
        }

        private static int StableId32(string s)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;
                for (int i = 0; i < s.Length; i++) { hash ^= s[i]; hash *= fnvPrime; }
                return (int)hash;
            }
        }

        private static string NormalizeTypeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            int i = 0;
            while (true)
            {
                int lt = name.IndexOf('<', i);
                if (lt < 0) break;
                int gt = name.IndexOf('>', lt + 1);
                if (gt < 0) break;
                var seg = name.Substring(lt, gt - lt + 1);
                int nextLt = name.IndexOf('<', gt + 1);
                if (nextLt == gt + 1)
                {
                    int nextGt = name.IndexOf('>', nextLt + 1);
                    if (nextGt > nextLt)
                    {
                        var seg2 = name.Substring(nextLt, nextGt - nextLt + 1);
                        if (string.Equals(seg, seg2, StringComparison.Ordinal)) { name = name.Remove(nextLt, seg2.Length); i = lt + seg.Length; continue; }
                    }
                }
                i = gt + 1;
            }
            return name;
        }

        private static string GetNamespaceFromFullTypeName(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx > 0 ? fullType.Substring(0, idx) : string.Empty;
        }

        private static string GetTypeShortName(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx >= 0 && idx + 1 < fullType.Length ? fullType.Substring(idx + 1) : fullType;
        }
    }
}
