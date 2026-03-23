using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using SimpleLanuageVM.Load;
using SimpleLanguage.Parse;

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

        public static SLAssemblyPackage ReadModule(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<SLAssemblyPackage>(json, options) ?? new SLAssemblyPackage();
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

            SLModulePackage pkg;
            using (var doc = JsonDocument.Parse(json))
            {
                if (TryGetJsonArrayLength(doc.RootElement, "moduleList", out _))
                {
                    // Front export root: SLPackageRootJson (entryModule + moduleList only).
                    var root = JsonSerializer.Deserialize<SLPackageRootJson>(json, options) ?? new SLPackageRootJson();
                    pkg = SLPackageRootMapping.ToModulePackage(root);
                }
                else
                {
                    pkg = JsonSerializer.Deserialize<SLModulePackage>(json, options) ?? new SLModulePackage();
                }
            }

            NormalizeModulePackageModel(pkg);
            NormalizeFieldFlags(pkg);
            return pkg;
        }

        /// <summary>Case-insensitive property lookup for JSON root inspection.</summary>
        private static bool TryGetJsonArrayLength(JsonElement root, string name, out int length)
        {
            length = 0;
            if (root.ValueKind != JsonValueKind.Object) return false;
            foreach (var p in root.EnumerateObject())
            {
                if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                if (p.Value.ValueKind != JsonValueKind.Array) return false;
                length = p.Value.GetArrayLength();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Root JSON may be only <c>entryModule</c> + <c>moduleList</c>. Fill <see cref="SLModulePackage.moduleName"/> for legacy code
        /// and keep <see cref="SLModulePackage.entryModule"/> in sync when missing.
        /// </summary>
        private static void NormalizeModulePackageModel(SLModulePackage? pkg)
        {
            if (pkg == null) return;
            if (pkg.moduleList == null || pkg.moduleList.Count == 0) return;

            if (string.IsNullOrEmpty(pkg.entryModule))
            {
                var first = pkg.moduleList[0];
                if (first != null && !string.IsNullOrEmpty(first.moduleName))
                    pkg.entryModule = first.moduleName;
            }

            if (string.IsNullOrEmpty(pkg.moduleName))
            {
                pkg.moduleName = !string.IsNullOrEmpty(pkg.entryModule)
                    ? pkg.entryModule!
                    : (pkg.moduleList[0]?.moduleName ?? string.Empty);
            }
        }

        private static void NormalizeFieldFlags(SLModulePackage? pkg)
        {
            if (pkg == null) return;
            NormalizeFieldFlagsForClassList(pkg.classList);
            if (pkg.moduleList != null)
            {
                for (int mi = 0; mi < pkg.moduleList.Count; mi++)
                {
                    NormalizeFieldFlagsForClassList(pkg.moduleList[mi]?.classList);
                }
            }
        }

        private static void NormalizeFieldFlagsForClassList(List<SLClassPackage>? classList)
        {
            if (classList == null) return;
            for (int c = 0; c < classList.Count; c++)
            {
                var cls = classList[c];
                if (cls?.fieldList == null) continue;
                for (int f = 0; f < cls.fieldList.Count; f++)
                {
                    var field = cls.fieldList[f];
                    if (field == null) continue;
                    const int allowed = 1 | 2 | 4 | 8 | 16 | 32;
                    field.flags &= allowed;
                }
            }
        }
        public static SLAssembly BuildRuntimeModel(SLModulePackage pkg)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));

            // Build VM assembly that contains one or more assembly packages built from SLModulePackage
            var asm = new SLAssembly("SimpleLanguage");

            // If front exported multiple physical modules, pkg.moduleList will contain SLAssemblyPackage entries.
            if (pkg.moduleList != null && pkg.moduleList.Count > 0)
            {
                foreach (var m in pkg.moduleList)
                {
                    if (m == null) continue;
                    asm.AddModule(m);
                }
            }
            else
            {
                // legacy single-module shape: construct an assembly package from top-level fields
                var module = new SLAssemblyPackage(pkg.moduleName);
                if (pkg.namespaceList != null) module.namespaceList.AddRange(pkg.namespaceList);
                if (pkg.classList != null) module.classList.AddRange(pkg.classList);
                if (pkg.globalStaticVariableList != null) module.globalStaticVariableList.AddRange(pkg.globalStaticVariableList);
                if (pkg.methodList != null) module.methodList.AddRange(pkg.methodList);
                asm.AddModule(module);
            }

            // Build a global namespace/type map across all modules to place global methods and per-class refs
            var nsMap = new Dictionary<string, SLNamespacePackage>(StringComparer.Ordinal);
            var typeMap = new Dictionary<string, SLTypePackage>(StringComparer.Ordinal);

            foreach (var module in asm.moduleList)
            {
                if (module.namespaceList == null) continue;
                foreach (var nsPkg in module.namespaceList)
                {
                    if (nsPkg == null) continue;
                    var fullNs = nsPkg.fullName ?? string.Empty;
                    if (!nsMap.TryGetValue(fullNs, out var ns))
                    {
                        ns = new SLNamespacePackage(fullNs);
                        nsMap[fullNs] = ns;
                    }
                    if (nsPkg.typeList != null)
                    {
                        foreach (var t in nsPkg.typeList)
                        {
                            if (t == null) continue;
                            var full = NormalizeTypeName(t.fullName);
                            var tp = new SLTypePackage { name = GetTypeShortName(full), fullName = full };
                            ns.typeList.Add(tp);
                            if (!typeMap.ContainsKey(tp.fullName)) typeMap[tp.fullName] = tp;
                        }
                    }
                    // ensure the module keeps its namespace objects as well
                    // (we don't remove existing module.namespaceList entries)
                }
            }

            // Place global method bodies into the corresponding types
            foreach (var module in asm.moduleList)
            {
                if (module.methodList == null) continue;
                foreach (var m in module.methodList)
                {
                    if (m == null) continue;
                    var declType = NormalizeTypeName(m.declaringTypeFullName);
                    if (!typeMap.TryGetValue(declType ?? string.Empty, out var tm))
                    {
                        var nsName = GetNamespaceFromFullTypeName(declType);
                        if (!nsMap.TryGetValue(nsName, out var ns))
                        {
                            ns = new SLNamespacePackage(nsName);
                            nsMap[nsName] = ns;
                        }
                        tm = new SLTypePackage { name = GetTypeShortName(declType), fullName = declType ?? string.Empty };
                        ns.typeList.Add(tm);
                        typeMap[tm.fullName] = tm;
                    }
                    var vmIns = SLIRModuleParse.ConvertToVMInstructionList(m.instructionList);
                    tm.AddMethod(new SLMethodMetaPackage { id = m.id ?? string.Empty, name = m.name ?? string.Empty, irList = new List<object>(), vmInstructionList = vmIns });
                }
            }

            // Process per-class method reference lists on each module's class packages
            foreach (var module in asm.moduleList)
            {
                if (module.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c == null) continue;
                    var cfull = NormalizeTypeName(c.fullName);
                    if (!typeMap.TryGetValue(cfull ?? string.Empty, out var tm)) continue;

                    if (c.nonStaticMethodList != null)
                    {
                        for (int i = 0; i < c.nonStaticMethodList.Count; i++)
                        {
                            var mm = c.nonStaticMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodMetaPackage { id = mm.id ?? string.Empty, name = mm.name ?? string.Empty, index = mm.index, irList = new List<object>(), vmInstructionList = new List<Instruction>() });
                        }
                    }

                    if (c.operatorMethodList != null)
                    {
                        for (int i = 0; i < c.operatorMethodList.Count; i++)
                        {
                            var mm = c.operatorMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodMetaPackage { id = mm.id ?? string.Empty, name = mm.name ?? string.Empty, index = mm.index, irList = new List<object>(), vmInstructionList = new List<Instruction>() });
                        }
                    }

                    if (c.staticMethodList != null)
                    {
                        for (int i = 0; i < c.staticMethodList.Count; i++)
                        {
                            var mm = c.staticMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodMetaPackage { id = mm.id ?? string.Empty, name = mm.name ?? string.Empty, index = mm.index, irList = new List<object>(), vmInstructionList = new List<Instruction>() });
                        }
                    }
                }
            }

            return asm;
        }

        public static SLModulePackage ReadPackage(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            if (!jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SLIRJsonModuleLoader.ReadPackage only supports module.package.json");
            return LoadFromJson(jsonPath);
        }

        public static SLPackageGraph ReadPackagesInExecutionOrder(string rootPackagePath)
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
                // Legacy: root moduleReferences. New format: references may live on each SLAssemblyPackage in moduleList.
                void LoadRefList(List<string>? list)
                {
                    if (list == null) return;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var rp = list[i];
                        if (string.IsNullOrWhiteSpace(rp)) continue;
                        var refPath = Path.IsPathRooted(rp) ? rp : Path.Combine(dir, rp);
                        LoadRecursive(refPath);
                    }
                }

                LoadRefList(pkg.moduleReferences);
                if (pkg.moduleList != null)
                {
                    for (int mi = 0; mi < pkg.moduleList.Count; mi++)
                    {
                        LoadRefList(pkg.moduleList[mi]?.moduleReferences);
                    }
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
            return new SLPackageGraph { rootPackagePath = rootFullPath, rootDirectory = Path.GetDirectoryName(rootFullPath) ?? string.Empty, packageList = result };
        }
        public static string GetDefaultJsonPath()
        {
            var outDir = Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (string.IsNullOrWhiteSpace(outDir))
                outDir = Path.Combine(Environment.CurrentDirectory, "out", "export");
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
            for (int i = 0; i < m.classList.Count; i++)
            {
                var c = m.classList[i];
                var rc = new RuntimeClass { id = StableId32(c.name), name = c.name ?? string.Empty };
                rcm.m_IRMetaClassList.Add(rc);
            }
            for (int i = 0; i < rcm.m_IRMetaClassList.Count; i++)
            {
                var rc = rcm.m_IRMetaClassList[i];
                if (rc == null) continue;
                if (RuntimeTypeManager.GetRuntimeTypeByClassId(rc.id) == null) RuntimeTypeManager.AddRuntimeTypeByClass(rc);
            }
            for (int i = 0; i < m.classList.Count; i++)
            {
                var c = m.classList[i];
                var rc = rcm.GetRuntimeClassByName(c.name);
                if (rc == null) continue;
                foreach (var f in c.fieldList )
                {
                    var rv = new RuntimeVariable();
                    //if (f.isStatic) rc.staticIRMetaVariableList.Add(rv); else rc.localIRMetaVariableList.Add(rv);
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
