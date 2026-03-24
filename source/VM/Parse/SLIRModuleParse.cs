
using SimpleLanguage.Parse;
using SimpleLanuageVM.Load;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    public static class SLIRModuleParse
    {
        // Multi-module data is normalized into SimpleLanguage.VM.SLAssembly.moduleList by SLIRJsonModuleLoader.BuildRuntimeModel.
        // Use SLAssembly.EnumerateGlobalStaticVariables / EnumerateClasses for globals and classes.

        //public static string? ResolvePackagePath(string[] args)
        //{
        //    var path = SLIRJsonModuleLoader.ResolveJsonPath(args);
        //    if (string.IsNullOrWhiteSpace(path)) return null;
        //    return path.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase) ? path : null;
        //}

        //public static SLIRModuleParseResult? Parse(string packagePath, string[] args)
        //{
        //    var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(packagePath);
        //    return Parse(graph, args);
        //}

        //public static SLIRModuleParseResult? Parse(string packagePath, SLModulePackage rootPackage, string[] args)
        //{
        //    if (rootPackage == null) return null;
        //    var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(packagePath);
        //    return Parse(graph, args);
        //}

        public static SLIRModuleParseResult? Parse(SLPackageGraph graph, string[] args)
        {
            if (graph == null) return null;

            var packageList = graph.packageList;
            if (packageList.Count == 0) return null;

            IntegrateConstStringDict(packageList);

            var currentPkg = packageList[packageList.Count - 1];

            SLRuntimeModuleRegistry.LoadFromPackages(packageList);
            var asmList = packageList.Select(p => SLIRJsonModuleLoader.BuildRuntimeModel(p)).ToList();
            var slAsm = asmList[asmList.Count - 1];

            //LoadBridgeMetadata(graph.rootDirectory);

            var (globalVarCount, globalInitCount) = InitializeGlobalVariables(asmList);

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

        private static void IntegrateConstStringDict(List<SLPackageRootJson> packageList)
        {
            var dict = new Dictionary<int, string>();
            if (packageList == null)
            {
                SLAssembly.SetConstStringDict(dict);
                return;
            }

            for (int i = 0; i < packageList.Count; i++)
            {
                var pkg = packageList[i];
                if (pkg == null) continue;

                void MergeIrStrings(IReadOnlyList<SimpleLanuageVM.Load.IRStringItem>? list)
                {
                    if (list == null) return;
                    for (int j = 0; j < list.Count; j++)
                    {
                        var item = list[j];
                        if (item == null) continue;
                        dict[item.id] = item.value ?? string.Empty;
                    }
                }

                if (pkg.moduleList != null)
                {
                    for (int m = 0; m < pkg.moduleList.Count; m++)
                    {
                        MergeIrStrings(pkg.moduleList[m]?.irStringDict);
                    }
                }
            }

            SLAssembly.SetConstStringDict(dict);
        }

        //private static void LoadBridgeMetadata(string packageDirectory)
        //{
        //    var bridgePath = Environment.GetEnvironmentVariable("SIMPLELANG_BRIDGE_JSON");
        //    if (string.IsNullOrWhiteSpace(bridgePath))
        //    {
        //        if (!string.IsNullOrEmpty(packageDirectory))
        //        {
        //            var guess = Path.Combine(packageDirectory, "ImportCSharpLang.json");
        //            if (File.Exists(guess)) bridgePath = guess;
        //        }
        //    }
        //    if (!string.IsNullOrWhiteSpace(bridgePath) && File.Exists(bridgePath))
        //    {
        //        SimpleLanguage.VM.Runtime.CSharpBridgeRegistry.LoadFromJson(bridgePath);
        //    }
        //}

        private static (int globalVariableCount, int globalInitInstructionCount) InitializeGlobalVariables(List<SLAssembly> assemblyList)
        {
            SimpleLanguage.VM.Runtime.CLRVM.ResetGlobalVariableMapping();

            var allGlobalInitInstructions = new List<Instruction>();
            int globalVarCount = 0;
            var globalFieldIdMap = new Dictionary<string, int>(StringComparer.Ordinal);

            // 1) Register every global static variable and build ownerClassId:index -> gv.id map (data from SLAssembly.moduleList).
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                foreach (var gv in asm.EnumerateGlobalStaticVariables())
                {
                    globalVarCount++;
                    var typeName = gv.typeDef != null ? gv.typeDef.className : string.Empty;
                    SimpleLanguage.VM.Runtime.CLRVM.RegisterGlobalVariable(gv.id, typeName, gv.ownerClassId, gv.index);
                    globalFieldIdMap[$"{gv.ownerClassId}:{gv.index}"] = gv.id;
                }
            }

            // 2) After registration, one merged init sequence: gv.express then StoreGlobal per global.
            var initializedGlobalIds = new HashSet<int>();
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                foreach (var gv in asm.EnumerateGlobalStaticVariables())
                {
                    if (gv.express != null && gv.express.Count > 0)
                    {
                        Instruction.UnpackPayloadsFromJson(gv.express);
                        allGlobalInitInstructions.AddRange(gv.express);
                        allGlobalInitInstructions.Add(new Instruction
                        {
                            opCode = EIROpCode.StoreGlobal,
                            index = gv.id,
                            opValue = null,
                            Payload = Array.Empty<byte>(),
                        });
                        initializedGlobalIds.Add(gv.id);
                    }
                }
            }

            // 3) Fallback: const fields with express only on class (matches global id via map).
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                foreach (var cls in asm.EnumerateClasses())
                {
                    if (cls.fieldList == null) continue;
                    for (int f = 0; f < cls.fieldList.Count; f++)
                    {
                        var field = cls.fieldList[f];
                        if (field == null) continue;
                        bool isConstField = ((field.flags & 16) == 16);
                        if (!isConstField) continue;
                        if (field.express == null || field.express.Count == 0) continue;
                        if (!globalFieldIdMap.TryGetValue($"{cls.id}:{field.index}", out var gid)) continue;
                        if (initializedGlobalIds.Contains(gid)) continue;

                        Instruction.UnpackPayloadsFromJson(field.express);
                        allGlobalInitInstructions.AddRange(field.express);
                        allGlobalInitInstructions.Add(new Instruction
                        {
                            opCode = EIROpCode.StoreGlobal,
                            index = gid,
                            opValue = null,
                            Payload = Array.Empty<byte>(),
                        });
                        initializedGlobalIds.Add(gid);
                    }
                }
            }

            SimpleLanguage.VM.Runtime.CLRVM.SetGlobalInitInstructions(allGlobalInitInstructions);
            SimpleLanguage.VM.Runtime.CLRVM.LoadGlobalVariableMapping();

            return (globalVarCount, allGlobalInitInstructions.Count);
        }

        private static string? ResolveEntryMethodId(SLPackageRootJson currentRoot, string[] args)
        {
            var entryId = Environment.GetEnvironmentVariable("SIMPLELANG_ENTRY_METHOD");
            if (!string.IsNullOrWhiteSpace(entryId))
                return entryId;

            bool runTest = args != null && args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
            if (runTest)
            {
                return FindMethodIdByName(currentRoot, "_test_");
            }

            // Canonical: select module by entryModule -> its entryMethodId.
            if (!string.IsNullOrWhiteSpace(currentRoot.entryModule))
            {
                for (int i = 0; i < currentRoot.moduleList.Count; i++)
                {
                    var m = currentRoot.moduleList[i];
                    if (m == null) continue;
                    if (string.Equals(m.moduleName, currentRoot.entryModule, StringComparison.Ordinal))
                    {
                        if (!string.IsNullOrWhiteSpace(m.entryMethodId))
                            return m.entryMethodId;
                    }
                }
            }

            // Fallback: first module's entryMethodId.
            return currentRoot.moduleList.Count > 0 ? currentRoot.moduleList[0]?.entryMethodId : null;
        }

        private static string? FindMethodIdByName(SLPackageRootJson root, string methodName)
        {
            if (root?.moduleList == null) return null;
            for (int mi = 0; mi < root.moduleList.Count; mi++)
            {
                var mod = root.moduleList[mi];
                if (mod?.methodList == null) continue;
                for (int m = 0; m < mod.methodList.Count; m++)
                {
                    var mm = mod.methodList[m];
                    if (mm != null && string.Equals(mm.name, methodName, StringComparison.OrdinalIgnoreCase))
                    {
                        return mm.id;
                    }
                }
            }

            return null;
        }


    }
}
