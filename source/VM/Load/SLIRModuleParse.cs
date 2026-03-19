using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public sealed class SLIRModuleParseResult
    {
        public List<SLModulePackage> packageList { get; init; } = new();
        public List<SLAssembly> assemblyList { get; init; } = new();
        public SLAssembly? assembly { get; init; }
        public SLModulePackage? currentPackage { get; init; }
        public string? entryMethodId { get; init; }
        public int globalVariableCount { get; init; }
        public int globalInitInstructionCount { get; init; }
    }

    public static class SLIRModuleParse
    {
        public static string? ResolvePackagePath(string[] args)
        {
            var path = SLIRJsonModuleLoader.ResolveJsonPath(args);
            if (string.IsNullOrWhiteSpace(path)) return null;
            return path.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        public static SLIRModuleParseResult? Parse(string packagePath, string[] args)
        {
            var rootPackage = SLIRJsonModuleLoader.ReadPackage(packagePath);
            return Parse(packagePath, rootPackage, args);
        }

        public static SLIRModuleParseResult? Parse(string packagePath, SLModulePackage rootPackage, string[] args)
        {
            if (rootPackage == null) return null;

            var packageList = LoadPackagesInExecutionOrder(packagePath, rootPackage);
            if (packageList.Count == 0) return null;

            var currentPkg = packageList[packageList.Count - 1];

            SLRuntimeModuleRegistry.LoadFromPackages(packageList);
            var asmList = packageList.Select(SLModulePackageLoader.BuildRuntimeModel).ToList();
            var slAsm = asmList[asmList.Count - 1];

            LoadBridgeMetadata(packagePath);

            var (globalVarCount, globalInitCount) = InitializeGlobalVariables(packageList);

            var entryId = ResolveEntryMethodId(currentPkg, args);

            return new SLIRModuleParseResult
            {
                packageList = packageList,
                assemblyList = asmList,
                assembly = slAsm,
                currentPackage = currentPkg,
                entryMethodId = entryId,
                globalVariableCount = globalVarCount,
                globalInitInstructionCount = globalInitCount,
            };
        }

        private static void LoadBridgeMetadata(string packagePath)
        {
            var bridgePath = Environment.GetEnvironmentVariable("SIMPLELANG_BRIDGE_JSON");
            if (string.IsNullOrWhiteSpace(bridgePath))
            {
                var dir = Path.GetDirectoryName(packagePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var guess = Path.Combine(dir, "ImportCSharpLang.json");
                    if (File.Exists(guess)) bridgePath = guess;
                }
            }
            if (!string.IsNullOrWhiteSpace(bridgePath) && File.Exists(bridgePath))
            {
                SimpleLanguage.VM.Runtime.CSharpBridgeRegistry.LoadFromJson(bridgePath);
            }
        }

        private static (int globalVariableCount, int globalInitInstructionCount) InitializeGlobalVariables(List<SLModulePackage> packageList)
        {
            SimpleLanguage.VM.Runtime.CLRVM.ResetGlobalVariableMapping();

            var allGlobalInitInstructions = new List<Instruction>();
            int globalVarCount = 0;
            var globalFieldIdMap = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < packageList.Count; i++)
            {
                var p = packageList[i];
                if (p.globalStaticVariableList != null)
                {
                    globalVarCount += p.globalStaticVariableList.Count;
                    for (int g = 0; g < p.globalStaticVariableList.Count; g++)
                    {
                        var gv = p.globalStaticVariableList[g];
                        SimpleLanguage.VM.Runtime.CLRVM.RegisterGlobalVariable(gv.id, gv.typeName, gv.ownerClassId, gv.index);
                        globalFieldIdMap[$"{gv.ownerClassId}:{gv.index}"] = gv.id;
                    }
                }

                if (p.classList != null && p.classList.Count > 0)
                {
                    for (int c = 0; c < p.classList.Count; c++)
                    {
                        var cls = p.classList[c];
                        if (cls?.fieldList == null) continue;

                        for (int f = 0; f < cls.fieldList.Count; f++)
                        {
                            var field = cls.fieldList[f];
                            if (field == null) continue;
                            if (!field.isConst && (field.flags & 16) == 16) field.isConst = true;
                            if (!field.isStatic && (field.flags & 32) == 32) field.isStatic = true;
                            bool isConstField = field.isConst || ((field.flags & 16) == 16);
                            if (!isConstField) continue;
                            if (field.express == null || field.express.Count == 0) continue;

                            allGlobalInitInstructions.AddRange(SLModulePackageLoader.ConvertToVMInstructionList(field.express));

                            if (globalFieldIdMap.TryGetValue($"{cls.id}:{field.index}", out var gid))
                            {
                                allGlobalInitInstructions.Add(new Instruction
                                {
                                    opCode = EIROpCode.StoreGlobal,
                                    index = gid,
                                    opValue = null,
                                    Payload = Array.Empty<byte>(),
                                });
                            }
                        }
                    }
                }
            }

            SimpleLanguage.VM.Runtime.CLRVM.SetGlobalInitInstructions(allGlobalInitInstructions);
            SimpleLanguage.VM.Runtime.CLRVM.LoadGlobalVariableMapping();

            return (globalVarCount, allGlobalInitInstructions.Count);
        }

        private static string? ResolveEntryMethodId(SLModulePackage currentPkg, string[] args)
        {
            var entryId = Environment.GetEnvironmentVariable("SIMPLELANG_ENTRY_METHOD");
            bool runTest = args != null && args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(entryId))
            {
                if (runTest)
                {
                    entryId = currentPkg.methodList?.FirstOrDefault(m => string.Equals(m.name, "_test_", StringComparison.OrdinalIgnoreCase))?.id;
                }
                else
                {
                    entryId = currentPkg.entryMethodId;
                }
            }

            return entryId;
        }

        private static List<SLModulePackage> LoadPackagesInExecutionOrder(string rootPackagePath, SLModulePackage? rootPackage = null)
        {
            var result = new List<SLModulePackage>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void LoadRecursive(string path)
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath)) return;
                if (!visited.Add(fullPath)) return;

                SLModulePackage pkg;
                if (rootPackage != null && string.Equals(fullPath, Path.GetFullPath(rootPackagePath), StringComparison.OrdinalIgnoreCase))
                {
                    pkg = rootPackage;
                }
                else
                {
                    pkg = SLIRJsonModuleLoader.ReadPackage(fullPath);
                }
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

            LoadRecursive(rootPackagePath);

            if (result.Count == 1)
            {
                var rootFullPath = Path.GetFullPath(rootPackagePath);
                var dir = Path.GetDirectoryName(rootFullPath) ?? string.Empty;
                var siblings = Directory.Exists(dir)
                    ? Directory.GetFiles(dir, "*.package.json")
                    : Array.Empty<string>();

                Array.Sort(siblings, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < siblings.Length; i++)
                {
                    var sp = Path.GetFullPath(siblings[i]);
                    if (string.Equals(sp, rootFullPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!visited.Add(sp)) continue;
                    result.Insert(result.Count - 1, SLIRJsonModuleLoader.ReadPackage(sp));
                }
            }

            return result;
        }
    }
}
