
using SimpleLanguage.Parse;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM
{
    public static class SLIRModuleParse
    {

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

        public static SLIRModuleParseResult? Parse( SLPackageGraph graph, string[] args)
        {
            if (graph == null) return null;

            var packageList = graph.packageList;
            if (packageList.Count == 0) return null;

            IntegrateConstStringDict(packageList);

            var currentPkg = packageList[packageList.Count - 1];

            SLRuntimeModuleRegistry.LoadFromPackages(packageList);
            var asmList = packageList.Select(p => SLIRJsonModuleLoader.BuildRuntimeModel(p)).ToList();
            var slAsm = asmList[asmList.Count - 1];

            LoadBridgeMetadata(graph.rootDirectory);

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

        private static void IntegrateConstStringDict(List<SLModulePackage> packageList)
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
                if (pkg?.irStringDict == null) continue;

                for (int j = 0; j < pkg.irStringDict.Count; j++)
                {
                    var item = pkg.irStringDict[j];
                    if (item == null) continue;
                    dict[item.id] = item.value ?? string.Empty;
                }
            }

            SLAssembly.SetConstStringDict(dict);
        }

        private static void LoadBridgeMetadata(string packageDirectory)
        {
            var bridgePath = Environment.GetEnvironmentVariable("SIMPLELANG_BRIDGE_JSON");
            if (string.IsNullOrWhiteSpace(bridgePath))
            {
                if (!string.IsNullOrEmpty(packageDirectory))
                {
                    var guess = Path.Combine(packageDirectory, "ImportCSharpLang.json");
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
                        var typeName = gv.typeDef != null ? gv.typeDef.className : string.Empty;
                        SimpleLanguage.VM.Runtime.CLRVM.RegisterGlobalVariable(gv.id, typeName, gv.ownerClassId, gv.index);
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
                            bool isConstField = ((field.flags & 16) == 16);
                            if (!isConstField) continue;
                            if (field.express == null || field.express.Count == 0) continue;

                            allGlobalInitInstructions.AddRange(ConvertToVMInstructionList(field.express));

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

        internal static List<SimpleLanguage.VM.Instruction> ConvertToVMInstructionList(List<SLIRInstructionPackage> list)
        {
            var result = new List<SimpleLanguage.VM.Instruction>(list?.Count ?? 0);
            if (list == null) return result;
            foreach (var d in list)
            {
                var opCode = (SimpleLanguage.VM.EIROpCode)d.opCode;
                object? opValue = d.opValue;
                var ins = new SimpleLanguage.VM.Instruction
                {
                    id = d.id,
                    opCode = opCode,
                    opValue = opValue,
                    Payload = d.payload,
                    ByteLength = d.byteLength,
                    index = d.index,
                };
                result.Add(ins);
            }
            return result;
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


    }
}
