
using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using SimpleLanguage.VM.Runtime;
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

        public static SLIRModuleParseResult? Parse(SLIRRuntimeLoadModel loadModel, string[] args)
        {
            if (loadModel == null) return null;
            var packageList = loadModel.packageList;
            var asmList = loadModel.assemblyList;
            if (packageList.Count == 0 || asmList.Count == 0) return null;

            IntegrateConstStringDict(packageList);

            // 加载模块声明的原生 DLL（nativeDll 字段）。
            // 在模块执行前加载，确保 SystemCallExternalFunction 能找到注册的函数。
            LoadNativeDllsForModules(packageList, loadModel.rootDirectory);

            var currentPkg = loadModel.currentPackage ?? packageList[packageList.Count - 1];

            SLRuntimeModuleRegistry.LoadFromPackages(packageList);

            // Ensure core primitive runtime types (String/Int32/Float32/...) are registered
            // before any global initializer instructions can create objects.
            RuntimeTypeManager.EnsureCoreRuntimeTypesRegistered();

            var slAsm = loadModel.currentAssembly ?? asmList[asmList.Count - 1];

            //LoadBridgeMetadata(graph.rootDirectory);

            // Eagerly initialize class static fields in module dependency order FIRST.
            // packageList is already sorted by ReadPackagesInExecutionOrder (dependencies first),
            // so iterating asmList.moduleList in order guarantees that a module's static fields
            // are initialized after all its dependencies' static fields.
            // This must happen BEFORE InitializeGlobalVariables, because global init instructions
            // may reference class static fields (e.g. StoreStaticField), which would trigger
            // lazy static initialization in an uncontrolled order.
            InitializeClassStaticFields(asmList);

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
            SLAssembly.ClearModuleStringDicts();

            if (packageList == null)
            {
                SLAssembly.SetConstStringDict(dict);
                return;
            }

            for (int i = 0; i < packageList.Count; i++)
            {
                var pkg = packageList[i];
                if (pkg == null) continue;

                void MergeIrStrings(string moduleUUID, IReadOnlyList<SimpleLanuageVM.Load.IRStringItem>? list)
                {
                    if (list == null) return;
                    // Build per-module dict
                    Dictionary<int, string> modDict = null;
                    if (!string.IsNullOrEmpty(moduleUUID))
                    {
                        modDict = new Dictionary<int, string>();
                    }
                    for (int j = 0; j < list.Count; j++)
                    {
                        var item = list[j];
                        if (item == null) continue;
                        dict[item.id] = item.value ?? string.Empty;
                        if (modDict != null)
                        {
                            modDict[item.id] = item.value ?? string.Empty;
                        }
                    }
                    if (modDict != null)
                    {
                        SLAssembly.SetModuleStringDict(moduleUUID, modDict);
                    }
                }

                if (pkg.moduleList != null)
                {
                    for (int m = 0; m < pkg.moduleList.Count; m++)
                    {
                        var mod = pkg.moduleList[m];
                        MergeIrStrings(mod?.uuid, mod?.irStringDict);
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

        public static void EntryPoint( SLIRModuleParseResult parseResult)
        {
            var entryId = parseResult.entryMethodId;

            // 2) run Main/entry of current module
            if (!string.IsNullOrWhiteSpace(entryId))
            {
                var rm = SLRuntimeModuleRegistry.GetMethod(entryId);
                if (rm == null)
                {
                    Log.AddProjectLog(LID.ShowMessageError, entryId );
                }
                else
                {
                    RuntimeVM vm = new RuntimeVM(null, new List<RuntimeType>(), rm);
                    CLRVM.PushCLRRuntime(vm);
                    vm.Run(true);
                    CLRVM.PopCLRRuntime();
                    // If the entry point has an uncaught exception, log it
                    if (vm.hasPendingException)
                    {
                        var ex = vm.pendingException;
                        string exMsg = ex.stringValue ?? ex.ToString();
                        Log.AddRuntimeLog(LID.ShowMessageAssert, null, "Uncaught exception: " + exMsg);
                    }
                }
            }
        }
        private static (int globalVariableCount, int globalInitInstructionCount) InitializeGlobalVariables(List<SLAssembly> assemblyList)
        {
            SimpleLanguage.VM.Runtime.CLRVM.ResetGlobalVariableMapping();

            int globalVarCount = 0;
            int globalInitCount = 0;
            var globalFieldIdMap = new Dictionary<string, int>(StringComparer.Ordinal);

            // 1) Register every global static variable and build ownerClassId:index -> gv.id map.
            //    Also cache (classId -> moduleUUID) so Phase 3 const-field fallback knows the module.
            var classIdToModuleUUID = new Dictionary<int, string>();
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                for (int mi = 0; mi < asm.moduleList.Count; mi++)
                {
                    var mod = asm.moduleList[mi];
                    if (mod == null) continue;

                    // Cache class -> module mapping for Phase 3 fallback
                    if (mod.classList != null)
                    {
                        for (int ci = 0; ci < mod.classList.Count; ci++)
                        {
                            var c = mod.classList[ci];
                            if (c != null && !classIdToModuleUUID.ContainsKey(c.id))
                                classIdToModuleUUID[c.id] = mod.uuid;
                        }
                    }

                    if (mod.globalStaticVariableList == null) continue;
                    for (int gi = 0; gi < mod.globalStaticVariableList.Count; gi++)
                    {
                        var gv = mod.globalStaticVariableList[gi];
                        if (gv == null) continue;
                        globalVarCount++;
                        globalFieldIdMap[$"{gv.ownerClassId}:{gv.index}"] = gv.id;

                        // Register in CLRVM so StoreGlobal/LoadGlobal can resolve the owning RuntimeType.
                        // The runtimeDefType points to the owning class (not the variable's type),
                        // because GetRuntimeTypeByDefTypeAndAdd must return the RuntimeType that
                        // holds the static member slot.
                        if (gv.ownerClassId != 0)
                        {
                            var rc = RuntimeClassManager.GetRuntimeClassById(gv.ownerClassId);
                            if (rc != null)
                            {
                                var rdt = new RuntimeDefType(rc);
                                var rv = new RuntimeVariable(rdt, gv.id, gv.index, gv.name ?? string.Empty, null);
                                SimpleLanguage.VM.Runtime.CLRVM.RegisterGlobalVariable((uint)gv.id, rv);
                            }
                        }
                    }
                }
            }

            // 2) Per-module init: each module's globals + const-field fallback run
            //    with the correct moduleUUID so LoadConstString resolves properly.
            var initializedGlobalIds = new HashSet<int>();
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                for (int mi = 0; mi < asm.moduleList.Count; mi++)
                {
                    var mod = asm.moduleList[mi];
                    if (mod == null) continue;

                    var moduleInitInstructions = new List<Instruction>();

                    // Phase 2a: this module's global static variables
                    if (mod.globalStaticVariableList != null)
                    {
                        for (int gi = 0; gi < mod.globalStaticVariableList.Count; gi++)
                        {
                            var gv = mod.globalStaticVariableList[gi];
                            if (gv == null) continue;
                            if (gv.express == null || gv.express.Count == 0) continue;

                            foreach (var ins in gv.express)
                            {
                                if (ins != null) ins.ExtractIndexFromPayload();
                                moduleInitInstructions.Add(ins);
                            }
                            moduleInitInstructions.Add(new Instruction
                            {
                                opCode = EIROpCode.StoreGlobal,
                                index = gv.id,
                                opValue = null,
                                Payload = Array.Empty<byte>(),
                            });
                            initializedGlobalIds.Add(gv.id);
                        }
                    }

                    // Phase 2b: const-field fallback for this module's classes
                    if (mod.classList != null)
                    {
                        for (int ci = 0; ci < mod.classList.Count; ci++)
                        {
                            var cls = mod.classList[ci];
                            if (cls == null || cls.fieldList == null) continue;
                            for (int f = 0; f < cls.fieldList.Count; f++)
                            {
                                var field = cls.fieldList[f];
                                if (field == null) continue;
                                bool isConstField = ((field.flags & 16) == 16);
                                if (!isConstField) continue;
                                if (field.express == null || field.express.Count == 0) continue;
                                if (!globalFieldIdMap.TryGetValue($"{cls.id}:{field.index}", out var gid)) continue;
                                if (initializedGlobalIds.Contains(gid)) continue;

                                foreach (var ins in field.express)
                                {
                                    if (ins != null) ins.ExtractIndexFromPayload();
                                    moduleInitInstructions.Add(ins);
                                }
                                moduleInitInstructions.Add(new Instruction
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

                    if (moduleInitInstructions.Count > 0)
                    {
                        SimpleLanguage.VM.Runtime.CLRVM.AddModuleGlobalInitInstructions(mod.uuid, mod.moduleName ?? string.Empty, moduleInitInstructions);
                        globalInitCount += moduleInitInstructions.Count;
                    }
                }
            }

            SimpleLanguage.VM.Runtime.CLRVM.LoadGlobalVariableMapping();

            return (globalVarCount, globalInitCount);
        }

        /// <summary>
        /// Eagerly initializes class static fields in module dependency order.
        /// Iterates assemblies -> modules -> classes, triggering EnsureStaticMemberObjectsInitialized
        /// on each class's RuntimeType. The dedup in ApplyStaticMemberExpressionsBatch ensures
        /// <summary>
        /// 扫描所有已加载模块的 nativeDll 字段，自动加载对应的原生 DLL。
        /// DLL 中需实现 ISLExternalFunctionModule 接口来注册外部函数。
        /// DLL 搜索顺序：模块 JSON 文件同目录 -> rootDirectory -> VM 运行目录/external/
        /// </summary>
        private static void LoadNativeDllsForModules(List<SLPackageRootJson> packageList, string rootDirectory)
        {
            if (packageList == null) return;
            var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in packageList)
            {
                if (root == null) continue;
                foreach (var mod in root.moduleList)
                {
                    if (mod == null || string.IsNullOrWhiteSpace(mod.nativeDll)) continue;
                    if (loaded.Contains(mod.nativeDll)) continue;

                    var dllPath = ResolveNativeDllPath(mod.nativeDll, root, rootDirectory);
                    if (dllPath != null)
                    {
                        var count = SimpleLanguage.VM.Runtime.VMExternalFunctionRegistry.LoadDll(dllPath);
                        SimpleLanguage.Logging.Log.AddProjectLog(
                            SimpleLanguage.Logging.LID.ShowMessageInfo,
                            $"[NativeDll] Loaded '{mod.nativeDll}' for module '{mod.moduleName}': {count} functions registered");
                    }
                    loaded.Add(mod.nativeDll);
                }
            }
        }

        private static string? ResolveNativeDllPath(string dllName, SLPackageRootJson root, string rootDirectory)
        {
            // 1. 模块 JSON 文件同目录
            if (!string.IsNullOrEmpty(root?.sourcePath))
            {
                var p = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(root.sourcePath) ?? "", dllName);
                if (System.IO.File.Exists(p)) return p;
            }
            // 2. rootDirectory
            if (!string.IsNullOrEmpty(rootDirectory))
            {
                var p = System.IO.Path.Combine(rootDirectory, dllName);
                if (System.IO.File.Exists(p)) return p;
            }
            // 3. VM 运行目录/external/
            var autoDir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "external");
            var autoPath = System.IO.Path.Combine(autoDir, dllName);
            if (System.IO.File.Exists(autoPath)) return autoPath;

            SimpleLanguage.Logging.Log.AddProjectLog(
                SimpleLanguage.Logging.LID.ShowMessageWarning,
                $"[NativeDll] '{dllName}' not found in module dir, rootDirectory, or external/");
            return null;
        }

        /// <summary>
        /// Eagerly initialize class static fields in module dependency order.
        /// each class is initialized exactly once, even if later accessed lazily.
        /// </summary>
        private static void InitializeClassStaticFields(List<SLAssembly> assemblyList)
        {
            for (int i = 0; i < assemblyList.Count; i++)
            {
                var asm = assemblyList[i];
                if (asm == null) continue;
                for (int mi = 0; mi < asm.moduleList.Count; mi++)
                {
                    var mod = asm.moduleList[mi];
                    if (mod?.classList == null) continue;
                    for (int ci = 0; ci < mod.classList.Count; ci++)
                    {
                        var cls = mod.classList[ci];
                        if (cls == null) continue;

                        var rc = RuntimeClassManager.GetRuntimeClassById(cls.id);
                        if (rc == null) continue;

                        // 模板类（泛型类）不需要进行静态字段初始化，
                        // 静态字段属于具体实例化的类型，而非模板定义本身。
                        if (rc.templateCount > 0) continue;

                        // Get or create the RuntimeType, then trigger static field initialization.
                        // ApplyStaticMemberExpressionsBatch uses the moduleUUID from
                        // SLRuntimeModuleRegistry.GetModuleUUIDByClassId for correct LoadConstString resolution.
                        var rdt = new RuntimeDefType(rc);
                        var rt = RuntimeTypeManager.GetRuntimeTypeByDefTypeAndAdd(rdt);
                        if (rt != null)
                        {
                            rt.EnsureStaticMemberObjectsInitialized();
                        }
                    }
                }
            }
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
