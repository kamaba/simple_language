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
        /// <summary>
        /// SLIR JSON 中每条指令反序列化为 <see cref="Instruction"/>：其可序列化成员多为 public 字段（Payload、opCode 等），
        /// System.Text.Json 默认不写 public 字段，必须开启 <see cref="JsonSerializerOptions.IncludeFields"/>，
        /// 否则读入内存的指令会保持默认值（空 Payload、opCode=0）。
        /// </summary>
        public static JsonSerializerOptions CreateSlirPackageReadOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                IncludeFields = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
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

        public static SLPackageRootJson ReadModule(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            var json = File.ReadAllText(jsonPath);
            var options = CreateSlirPackageReadOptions();
            return JsonSerializer.Deserialize<SLPackageRootJson>(json, options) ?? new SLPackageRootJson();
        }

        // Merged helpers from SLModulePackageLoader
        public static SLPackageRootJson LoadFromJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            var json = File.ReadAllText(path);
            var options = CreateSlirPackageReadOptions();
            using (var doc = JsonDocument.Parse(json))
            {
                if (TryGetJsonArrayLength(doc.RootElement, "moduleList", out _))
                {
                    // Front export root: SLPackageRootJson (entryModule + moduleList only).
                    var root = JsonSerializer.Deserialize<SLPackageRootJson>(json, options) ?? new SLPackageRootJson();
                    if (string.IsNullOrWhiteSpace(root.entryModule) && root.moduleList.Count > 0)
                    {
                        root.entryModule = root.moduleList[0]?.moduleName ?? string.Empty;
                    }
                    if (root.moduleList != null)
                    {
                        for (int mi = 0; mi < root.moduleList.Count; mi++)
                        {
                            NormalizeFieldFlagsForClassList(root.moduleList[mi]?.classList);
                        }
                    }
                    return root;
                }
                else
                {
                    // Legacy single-module shape: convert it into canonical root wrapper.
                    var legacy = JsonSerializer.Deserialize<SLModulePackage>(json, options) ?? new SLModulePackage();
                    NormalizeFieldFlagsForClassList(legacy.classList);
                    return new SLPackageRootJson
                    {
                        entryModule = legacy.moduleName ?? string.Empty,
                        moduleList = new List<SLModulePackage> { legacy },
                    };
                }
            }
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

        public static SLAssembly BuildRuntimeModel(SLPackageRootJson root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            // Build VM assembly that contains one or more assembly packages built from SLModulePackage
            var asm = new SLAssembly("SimpleLanguage");

            // Canonical export: root.moduleList contains physical modules.
            if (root.moduleList != null && root.moduleList.Count > 0)
            {
                foreach (var m in root.moduleList)
                {
                    if (m == null) continue;
                    asm.AddModule(m);
                }
            }

            // Important: Front JSON puts method bodies under module.methodList, while namespace/type nodes exist
            // but usually have empty methodList. VM runtime model must attach method bodies into the *existing*
            // SLTypePackage instances from module.namespaceList/typeList, otherwise Program's counting will be 0.
            var typeMap = new Dictionary<string, SLTypePackage>(StringComparer.Ordinal);

            // 1) Index existing types from module.namespaceList/typeList.
            foreach (var module in asm.moduleList)
            {
                if (module?.namespaceList == null) continue;
                foreach (var nsPkg in module.namespaceList)
                {
                    if (nsPkg?.typeList == null) continue;
                    foreach (var t in nsPkg.typeList)
                    {
                        if (t == null) continue;
                        var full = NormalizeTypeName(t.fullName);
                        t.fullName = full;
                        t.name = GetTypeShortName(full);
                        if (!typeMap.ContainsKey(full)) typeMap[full] = t;
                    }
                }
            }

            // 2) Attach method bodies from module.methodList into those indexed types.
            foreach (var module in asm.moduleList)
            {
                if (module?.methodList == null) continue;
                foreach (var m in module.methodList)
                {
                    if (m == null) continue;
                    var declType = NormalizeTypeName(m.declaringTypeFullName);
                    if (string.IsNullOrWhiteSpace(declType)) continue;

                    if (!typeMap.TryGetValue(declType, out var tm))
                    {
                        // Rare fallback: create missing type package and attach into a matching namespace.
                        var nsName = GetNamespaceFromFullTypeName(declType);
                        SLTypePackage? created = null;
                        foreach (var mod2 in asm.moduleList)
                        {
                            if (mod2?.namespaceList == null) continue;
                            foreach (var ns2 in mod2.namespaceList)
                            {
                                if (ns2 == null) continue;
                                if (!string.Equals(ns2.fullName, nsName, StringComparison.Ordinal)) continue;
                                created = new SLTypePackage { fullName = declType, name = GetTypeShortName(declType) };
                                ns2.typeList ??= new List<SLTypePackage>();
                                ns2.typeList.Add(created);
                                typeMap[declType] = created;
                                break;
                            }
                            if (created != null) break;
                        }
                        if (created == null)
                        {
                            var firstNs = asm.moduleList.FirstOrDefault()?.namespaceList?.FirstOrDefault();
                            if (firstNs != null)
                            {
                                created = new SLTypePackage { fullName = declType, name = GetTypeShortName(declType) };
                                firstNs.typeList ??= new List<SLTypePackage>();
                                firstNs.typeList.Add(created);
                                typeMap[declType] = created;
                            }
                        }
                        tm = created;
                    }

                    if (tm == null) continue;

                    tm.AddMethod(new SLMethodPackage
                    {
                        id = m.id ?? string.Empty,
                        name = m.name ?? string.Empty,
                        irList = new List<object>(),
                        instructionList = m.instructionList ?? new List<Instruction>(),
                    });
                }
            }

            // 3) Process per-class method reference lists on each module's class packages.
            // These are meta references; method bodies have already been attached via module.methodList above.
            foreach (var module in asm.moduleList)
            {
                if (module?.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c == null) continue;
                    var cfull = NormalizeTypeName(c.fullName);
                    if (string.IsNullOrWhiteSpace(cfull)) continue;
                    if (!typeMap.TryGetValue(cfull, out var tm)) continue;

                    if (c.nonStaticMethodList != null)
                    {
                        for (int i = 0; i < c.nonStaticMethodList.Count; i++)
                        {
                            var mm = c.nonStaticMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodPackage
                            {
                                id = mm.id ?? string.Empty,
                                name = mm.name ?? string.Empty,
                                index = mm.index,
                                irList = new List<object>(),
                                instructionList = new List<Instruction>(),
                            });
                        }
                    }

                    if (c.operatorMethodList != null)
                    {
                        for (int i = 0; i < c.operatorMethodList.Count; i++)
                        {
                            var mm = c.operatorMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodPackage
                            {
                                id = mm.id ?? string.Empty,
                                name = mm.name ?? string.Empty,
                                index = mm.index,
                                irList = new List<object>(),
                                instructionList = new List<Instruction>(),
                            });
                        }
                    }

                    if (c.staticMethodList != null)
                    {
                        for (int i = 0; i < c.staticMethodList.Count; i++)
                        {
                            var mm = c.staticMethodList[i];
                            if (mm == null) continue;
                            tm.AddMethod(new SLMethodPackage
                            {
                                id = mm.id ?? string.Empty,
                                name = mm.name ?? string.Empty,
                                index = mm.index,
                                irList = new List<object>(),
                                instructionList = new List<Instruction>(),
                            });
                        }
                    }
                }
            }

            return asm;
        }

        public static SLPackageRootJson ReadPackage(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath)) jsonPath = GetDefaultJsonPath();
            if (!jsonPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SLIRJsonModuleLoader.ReadPackage only supports module.package.json");
            return LoadFromJson(jsonPath);
        }

        public static SLPackageGraph ReadPackagesInExecutionOrder(string rootPackagePath)
        {
            if (string.IsNullOrWhiteSpace(rootPackagePath)) throw new ArgumentNullException(nameof(rootPackagePath));
            var rootFullPath = Path.GetFullPath(rootPackagePath);
            var result = new List<SLPackageRootJson>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void LoadRecursive(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath)) return;
                if (!visited.Add(fullPath)) return;
                var pkgRoot = ReadPackage(fullPath);
                var dir = Path.GetDirectoryName(fullPath) ?? string.Empty;
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

                if (pkgRoot?.moduleList != null)
                {
                    for (int mi = 0; mi < pkgRoot.moduleList.Count; mi++)
                    {
                        LoadRefList(pkgRoot.moduleList[mi]?.moduleReferences);
                    }
                }

                result.Add(pkgRoot);
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
            return new SLPackageGraph
            {
                rootPackagePath = rootFullPath,
                rootDirectory = Path.GetDirectoryName(rootFullPath) ?? string.Empty,
                packageList = result
            };
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
            // module.slir.json / legacy root: normalize into canonical wrapper first.
            var m = LoadFromJson(jsonPath);
            var allClasses = new List<SLClassPackage>();
            if (m?.moduleList != null)
            {
                for (int mi = 0; mi < m.moduleList.Count; mi++)
                {
                    var mod = m.moduleList[mi];
                    if (mod?.classList != null) allClasses.AddRange(mod.classList);
                }
            }
            var rcm = RuntimeClassManager.instance;
            rcm.m_IRMetaClassList.Clear();
            for (int i = 0; i < allClasses.Count; i++)
            {
                var c = allClasses[i];
                var rc = new RuntimeClass { id = StableId32(c.name), name = c.name ?? string.Empty };
                rcm.m_IRMetaClassList.Add(rc);
            }
            for (int i = 0; i < rcm.m_IRMetaClassList.Count; i++)
            {
                var rc = rcm.m_IRMetaClassList[i];
                if (rc == null) continue;
                if (RuntimeTypeManager.GetRuntimeTypeByClassId(rc.id) == null) RuntimeTypeManager.AddRuntimeTypeByClass(rc);
            }
            for (int i = 0; i < allClasses.Count; i++)
            {
                var c = allClasses[i];
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
