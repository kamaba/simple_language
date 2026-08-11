using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleLanguage.Project
{
    /// <summary>
    /// Loads referenced modules declared in the "references" section of a project's .jsonc config.
    /// Each reference has: path (relative or absolute), uuid (optional), name (the import alias).
    ///
    /// Loading strategy (tried in order):
    ///   1. Source module      (directory containing a .jsonc with struct tree)
    ///   2. Compiled package   (.package.json / .module.json) as fallback
    ///
    /// Source-first: the module's elements are loaded directly from source and
    /// are only available within the current compilation project (not re-exported).
    /// </summary>
    public static class ProjectReferenceModuleLoader
    {
        /// <summary>
        /// When true, loading is replacing the inner-form Core types in coreModule
        /// with compiled definitions. Existing types are reused and their members overwritten.
        /// </summary>
        private static bool s_isCoreReplacement = false;

        /// <summary>
        /// 已加载的引用模块包（按模块名查），供导出时填充 moduleReferences 使用。
        /// </summary>
        private static readonly Dictionary<string, SLModulePackage> s_loadedPackages = new(StringComparer.Ordinal);

        /// <summary>
        /// 按 moduleName 获取已加载的引用模块包（可能为 null）。
        /// </summary>
        public static SLModulePackage GetLoadedPackage(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName)) return null;
            return s_loadedPackages.TryGetValue(moduleName, out var pkg) ? pkg : null;
        }

        /// <summary>
        /// 按 uuid 获取已加载的引用模块包（可能为 null）。
        /// </summary>
        public static SLModulePackage GetLoadedPackageByUuid(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return null;
            foreach (var kv in s_loadedPackages)
            {
                if (kv.Value != null && kv.Value.uuid == uuid)
                    return kv.Value;
            }
            return null;
        }

        public static void LoadReferences(ProjectConfig config, string projectDir)
        {
            if (config?.References == null || config.References.Count == 0)
            {
                return;
            }

            for (int i = 0; i < config.References.Count; i++)
            {
                LoadReference(config.References[i], projectDir);
            }
        }

        private static void LoadReference(ProjectConfig.ReferenceSection reference, string projectDir)
        {
            if (reference == null || string.IsNullOrWhiteSpace(reference.Path))
            {
                return;
            }

            /* --- Strategy 1: Try compiled package (.module.json) in the reference
             * directory or in the export output directory.  Compiled packages have
             * actual class/interface definitions, not just empty struct shells. --- */
            var packagePath = ResolveReferenceModulePath(reference.Path, projectDir);
            if (!string.IsNullOrWhiteSpace(packagePath) && File.Exists(packagePath))
            {
                if (TryLoadCompiledPackage(reference, packagePath))
                {
                    return;
                }
            }

            /* --- Strategy 2: Try compiled package from the reference module's
             * export outputDir (e.g. out/export/Core/Core.module.json) --- */
            var exportPackagePath = TryResolveExportModulePath(reference, projectDir);
            if (!string.IsNullOrWhiteSpace(exportPackagePath) && File.Exists(exportPackagePath))
            {
                if (TryLoadCompiledPackage(reference, exportPackagePath))
                {
                    return;
                }
            }

            /* --- Strategy 3: Fallback to source module (.jsonc with struct tree) --- */
            if (TryLoadSourceModule(reference, projectDir))
            {
                return;
            }

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module not found or could not be loaded: path={reference.Path}, name={reference.Name}");
            Console.WriteLine($"[Reference] Failed to load module: name={reference.Name}, path={reference.Path}");
            return;
        }

        /// <summary>
        /// 将引用模块名注册为工程级类型别名，指向模块根 MetaNode。
        /// 这样 "Core.IIterable" 中的 "Core" 能通过 TryResolveTypeAlias 找到模块根节点，
        /// 再继续解析剩余路径。
        /// </summary>
        private static void RegisterModuleAlias(string moduleName, MetaModule metaModule)
        {
            if (string.IsNullOrWhiteSpace(moduleName) || metaModule == null)
                return;
            var moduleMt = new MetaType(metaModule);
            TypeManager.instance.AddProjectTypeAlias(moduleName, moduleMt);
        }

        /// <summary>
        /// Reads the referenced module's .jsonc to find its export.outputDir and
        /// constructs the path to the compiled .module.json file.
        /// </summary>
        private static string TryResolveExportModulePath(ProjectConfig.ReferenceSection reference, string projectDir)
        {
            try
            {
                var resolvedDir = Path.IsPathRooted(reference.Path)
                    ? Path.GetFullPath(reference.Path)
                    : Path.GetFullPath(Path.Combine(projectDir ?? string.Empty, reference.Path));

                if (!Directory.Exists(resolvedDir))
                    return null;

                var jsoncFiles = Directory.GetFiles(resolvedDir, "*.jsonc", SearchOption.TopDirectoryOnly);
                if (jsoncFiles.Length == 0)
                    return null;

                var refConfig = ProjectJsoncLoader.FromJsonc(File.ReadAllText(jsoncFiles[0]));
                if (refConfig?.Export == null || string.IsNullOrWhiteSpace(refConfig.Export.OutputDir) || string.IsNullOrWhiteSpace(refConfig.Export.ModuleName))
                    return null;

                var moduleJsonPath = Path.Combine(refConfig.Export.OutputDir, refConfig.Export.ModuleName, refConfig.Export.ModuleName + ".module.json");
                return File.Exists(moduleJsonPath) ? moduleJsonPath : null;
            }
            catch
            {
                return null;
            }
        }

        #region Compiled package loading

        private static bool TryLoadCompiledPackage(ProjectConfig.ReferenceSection reference, string modulePath)
        {
            SLModulePackage package;
            try
            {
                package = SLModulePackageWriter.ReadWithoutInstructionCode(modulePath);
            }
            catch (Exception ex)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    "Reference module read failed: " + modulePath + " " + ex.Message);
                return false;
            }

            //if (!ValidateUuid(reference, package, modulePath))
            //{
            //    return false;
            //}

            /* Register the referenced module's system call declarations (embedded
             * verbatim in the package at export time) into the FrontEnd registry. */
            if (!string.IsNullOrWhiteSpace(package.systemCallsJson))
            {
                int sysCount = SystemMethodCallDeclarationRegistry.LoadFromJsonContent(
                    "{\"systemCalls\":" + package.systemCallsJson + "}");
                if (sysCount > 0)
                {
                    Log.AddProjectLog(LID.ShowExtendMessage,
                        $"Reference module registered {sysCount} system calls. path={modulePath}");
                }
            }

            var alias = ResolveModuleName(reference, package, modulePath);

            /* 记录已加载的包，供导出时填充 moduleReferences 使用。 */
            if (!string.IsNullOrWhiteSpace(package.moduleName) && !s_loadedPackages.ContainsKey(package.moduleName))
            {
                s_loadedPackages.Add(package.moduleName, package);
            }

            /* Core module: reuse the existing coreModule (populated by CoreMetaClassManager.Init)
             * and overwrite inner-form types with compiled definitions.
             * Non-Core modules: skip if already loaded. */
            bool isCore = package.moduleName == "Core";
            MetaModule metaModule;

            if (isCore)
            {
                metaModule = ModuleManager.instance.coreModule;
                s_isCoreReplacement = true;
            }
            else
            {
                var existingModule = ModuleManager.instance.GetMetaModuleByName(package.moduleName);
                if (existingModule != null)
                {
                    Log.AddProjectLog(LID.ShowExtendMessage,
                        $"Reference module '{package.moduleName}' is already loaded, skipping. path={modulePath}");
                    Console.WriteLine($"[Reference] Module already loaded, skipping: name={package.moduleName}, path={modulePath}");
                    return true;
                }
                metaModule = new MetaModule(package.moduleName);
                metaModule.SetRefFromType(RefFromType.RefModule);
                s_isCoreReplacement = false;
            }

            BuildModuleTree(metaModule, package);

            if (!isCore)
            {
                ModuleManager.instance.AddMetaMdoule(metaModule);
            }
            else
            {
                // Ensure "Core" is discoverable by GetMetaModuleByName so that
                // qualified names like "Core.IIterable" can be resolved.
                if (ModuleManager.instance.GetMetaModuleByName("Core") == null)
                {
                    ModuleManager.instance.AddMetaMdoule(metaModule);
                }
            }
            s_isCoreReplacement = false;

            // 将模块名注册为工程级类型别名，指向模块根节点。
            // 这样 "Core.IIterable" 中的 "Core" 能通过 TryResolveTypeAlias 找到模块根 MetaNode，
            // 再继续解析 "IIterable" 等剩余路径。
            RegisterModuleAlias(alias, metaModule);

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module loaded (compiled): name={alias}, path={modulePath}");
            Console.WriteLine($"[Reference] Module loaded (compiled): name={alias}, path={modulePath}");
            return true;
        }

        private static bool ValidateUuid(ProjectConfig.ReferenceSection reference, SLModulePackage package, string modulePath)
        {
            if (string.IsNullOrWhiteSpace(reference.UUID))
            {
                return true;
            }

            var expected = reference.UUID.Trim();
            var actual = package?.uuid?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(actual) && package?.moduleList != null)
            {
                actual = package.moduleList.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m?.uuid))?.uuid?.Trim() ?? string.Empty;
            }

            if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module uuid mismatch: {modulePath}, expected={expected}, actual={actual}");
            return false;
        }

        private static string ResolveReferenceModulePath(string path, string projectDir)
        {
            var resolved = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(projectDir ?? string.Empty, path));

            if (File.Exists(resolved))
            {
                return resolved;
            }

            if (Directory.Exists(resolved))
            {
                var package = Directory.GetFiles(resolved, "*.package.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(package))
                {
                    return package;
                }

                var module = Directory.GetFiles(resolved, "*.module.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(module))
                {
                    return module;
                }
            }

            return resolved;
        }

        private static string ResolveModuleName(ProjectConfig.ReferenceSection reference, SLModulePackage package, string modulePath)
        {
            if (!string.IsNullOrWhiteSpace(reference.Name))
            {
                var name = reference.Name.Trim();
                // Strip file extensions like "Std.module.json" -> "Std"
                if (name.EndsWith(".module.json", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - ".module.json".Length);
                else if (name.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - ".package.json".Length);
                else if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - ".json".Length);
                return name;
            }

            if (!string.IsNullOrWhiteSpace(package?.moduleName))
            {
                return package.moduleName.Trim();
            }

            return Path.GetFileNameWithoutExtension(modulePath);
        }

        private static void BuildModuleTree(MetaModule metaModule, SLModulePackage package)
        {
            if (metaModule == null || package == null)
            {
                return;
            }

            if (package.moduleList != null && package.moduleList.Count > 0)
            {
                for (int i = 0; i < package.moduleList.Count; i++)
                {
                    var irMetaModule = BuildIRMetaModuleFromPackage(metaModule, package.moduleList[i]);
                    if (irMetaModule != null)
                    {
                        ProcessMetaModuleFromIR(metaModule, irMetaModule, package.moduleList[i]);
                    }
                }
                return;
            }

            var assemblyPackage = new SLAssemblyPackage
            {
                moduleName = package.moduleName,
                uuid = package.uuid,
                namespaceList = package.namespaceList,
                classList = package.classList,
                globalStaticVariableList = package.globalStaticVariableList,
                methodList = package.methodList,
            };
            var irMetaModule2 = BuildIRMetaModuleFromPackage(metaModule, assemblyPackage);
            if (irMetaModule2 != null)
            {
                ProcessMetaModuleFromIR(metaModule, irMetaModule2, assemblyPackage);
            }
        }

        /// <summary>
        /// Phase A（IR 层先行）：建立 IRMetaModule，用导出的逆方法从 SLClassPackage 反向读取
        /// 构建 IRMetaClass（注册到 IRManager）并填充字段/方法列表。此时不依赖 Meta 层。
        /// 返回 IRMetaModule 供后续 Meta 层处理使用。
        /// </summary>
        private static IRMetaModule BuildIRMetaModuleFromPackage(MetaModule metaModule, SLAssemblyPackage package)
        {
            if (metaModule == null || package == null)
            {
                return null;
            }

            /* Build method lookup: id -> SLMethodPackage for signature resolution */
            var methodLookup = new Dictionary<string, SLMethodPackage>();
            if (package.methodList != null)
            {
                foreach (var m in package.methodList)
                {
                    if (m != null && !string.IsNullOrWhiteSpace(m.id))
                    {
                        methodLookup[m.id] = m;
                    }
                }
            }

            if (package.namespaceList != null)
            {
                for (int i = 0; i < package.namespaceList.Count; i++)
                {
                    var nsFullName = package.namespaceList[i]?.fullName;
                    if (string.IsNullOrWhiteSpace(nsFullName)) continue;

                    // namespaceList 的 fullName 包含模块名前缀（如 "ProjectTest.ConStrCC"），
                    // 而 metaModule.metaNode 已经代表模块根，需要剥离模块名前缀。
                    var nsPath = nsFullName;
                    if (nsPath.StartsWith(metaModule.name + "."))
                        nsPath = nsPath.Substring(metaModule.name.Length + 1);
                    else if (nsPath == metaModule.name)
                        nsPath = string.Empty;  // 模块根命名空间，不需要创建子节点

                    if (!string.IsNullOrEmpty(nsPath))
                        EnsureNamespacePath(metaModule.metaNode, nsPath);
                }
            }

            if (package.classList == null)
            {
                return null;
            }

            var irMetaModule = new IRMetaModule(metaModule.name, methodLookup, s_isCoreReplacement);
            irMetaModule.CreateIRMetaClassesFromPackage(package.classList);
            irMetaModule.BuildAllMembersFromPackage(package.classList);
            return irMetaModule;
        }

        /// <summary>
        /// Phase B + C（Meta 层处理）：接收 IRMetaModule，处理 MetaModule，
        /// 查找/创建其中的 MetaClass（Meta shell），进行 IRMetaClass 到 MetaClass 的关联与成员构建。
        /// B1: 生成 MetaClass/MetaData/MetaEnum shell 并注册到命名空间树。
        /// C:  Link，使 IRMetaClass.typeOwner 指向 MetaBase。
        /// B2: 从 IRMetaClass 反向构建 Meta 成员（导出的逆方法）。
        /// </summary>
        private static void ProcessMetaModuleFromIR(MetaModule metaModule, IRMetaModule irMetaModule, SLAssemblyPackage package)
        {
            if (metaModule == null || irMetaModule == null || package?.classList == null)
            {
                return;
            }

            /* Build class lookup: id -> SLClassPackage for interface/inheritance resolution */
            var classLookup = new Dictionary<int, SLClassPackage>();
            foreach (var c in package.classList)
            {
                if (c != null) classLookup[c.id] = c;
            }

            /* Phase B1（Meta shell）：IR 全部建完后，生成 MetaClass/MetaData/MetaEnum shell，
             * 注册到命名空间树。此时不填充成员，保证所有类型 shell 先就位。 */
            var createdTypes = new List<(SLClassPackage cls, MetaBase metaBase)>();
            for (int i = 0; i < package.classList.Count; i++)
            {
                var cls = package.classList[i];
                if (cls == null) continue;
                var metaBase = CreateReferenceTypeShell(metaModule, cls);
                if (metaBase != null)
                {
                    createdTypes.Add((cls, metaBase));
                }
            }

            /* Phase C（关联）：先 Link，使 IRMetaClass.typeOwner 指向 MetaBase，
             * 之后 IRMetaType.ToMetaType 才能复原 MetaType。 */
            irMetaModule.LinkMetaOwners(createdTypes);

            /* Phase B2（反向构建 Meta 成员）：从 IRMetaClass 的 IRMetaVariable/IRMethod
             * 反向构建 MetaMemberVariable/Data/Enum 与 MetaMemberFunction（导出的逆方法）。
             * 基类 / 接口仍来自 SLClassPackage（IRMetaClass 不独立承载）。 */
            foreach (var (cls, metaBase) in createdTypes)
            {
                if (!irMetaModule.TryGetIRMetaClass(cls.id, out var irmc)) continue;
                PopulateReferenceTypeMembersFromIR(metaModule, irmc, cls, metaBase, classLookup);
            }
        }

        private static MetaNode EnsureNamespacePath(MetaNode root, string fullName)
        {
            if (root == null || string.IsNullOrWhiteSpace(fullName))
            {
                return root;
            }

            var current = root;
            var parts = fullName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var name = parts[i];
                var child = current.GetChildrenMetaNodeByName(name);
                if (child == null)
                {
                    child = current.AddMetaNamespace(new MetaNamespace(name));
                }
                current = child;
            }
            return current;
        }

        /// <summary>
        /// Pass 1: Creates an empty type shell (MetaClass/MetaData/MetaEnum) and registers
        /// it in the namespace tree. No members are populated yet.
        /// Returns the created MetaBase, or null if skipped.
        /// </summary>
        private static MetaBase CreateReferenceTypeShell(MetaModule metaModule, SLClassPackage cls)
        {
            if (metaModule == null || cls == null) return null;

            var fullName = !string.IsNullOrWhiteSpace(cls.fullName) ? cls.fullName : cls.name;
            if (string.IsNullOrWhiteSpace(fullName)) return null;

            var nsName = GetNamespace(fullName);
            // 剥离模块名前缀（metaModule.metaNode 已代表模块根）
            if (!string.IsNullOrEmpty(nsName) && nsName.StartsWith(metaModule.name + "."))
                nsName = nsName.Substring(metaModule.name.Length + 1);
            else if (nsName == metaModule.name)
                nsName = string.Empty;

            var typeName = !string.IsNullOrWhiteSpace(cls.name) ? cls.name : GetShortName(fullName);
            // Strip template suffix from typeName so that "IIterator<T>" and "IIterator"
            // register under the same MetaNode key "IIterator" (with different template counts).
            var ltIdx = typeName.IndexOf('<');
            if (ltIdx > 0)
            {
                typeName = typeName.Substring(0, ltIdx);
            }
            var parent = EnsureNamespacePath(metaModule.metaNode, nsName);
            if (parent == null) return null;

            /* Core replacement: first check CoreMetaClassManager for BaseMetaClass-registered
             * types (Object, Int32, Boolean, etc.). If found, return the existing inner-form
             * class so its members get overwritten in Pass 2 with the compiled definitions. */
            if (s_isCoreReplacement)
            {
                /* typeName already has template suffix stripped; use it directly
                 * for CoreMetaClassManager lookup (inner-form registers as "Array", not "Array<T>"). */
                var coreNode = CoreMetaClassManager.GetCoreMetaClass(typeName);
                if (coreNode != null)
                {
                    if (coreNode.isMetaData)
                    {
                        coreNode.metaData.SetRefFromType(RefFromType.RefModule);
                        return coreNode.metaData;
                    }
                    if (coreNode.isMetaEnum)
                    {
                        coreNode.metaEnum.SetRefFromType(RefFromType.RefModule);
                        return coreNode.metaEnum;
                    }
                    var existingMc = coreNode.GetMetaClassByTemplateCount(cls.templateParameterCount);
                    if (existingMc != null)
                    {
                        existingMc.SetRefFromType(RefFromType.RefModule);
                        return existingMc;
                    }
                }
            }

            /* Skip if a child node already exists (duplicate, non-Core or Core type
             * not in BaseMetaClass registry). */
            var existingChild = parent.GetChildrenMetaNodeByName(typeName);
            if (existingChild != null)
            {
                return null;
            }

            switch ((IRMetaClassKind)cls.metaClassKind)
            {
                case IRMetaClassKind.Data:
                {
                    var md = new MetaData(typeName, false, false, cls.isDynamic);
                    md.SetRefFromType(RefFromType.RefModule);
                    md.SetAllName(fullName);
                    md.SetClassDefineType(EClassDefineType.StructDefine);
                    parent.AddMetaData(md);
                    return md;
                }
                case IRMetaClassKind.Enum:
                {
                    var me = new MetaEnum(typeName);
                    me.SetRefFromType(RefFromType.RefModule);
                    me.SetClassDefineType(EClassDefineType.StructDefine);
                    parent.AddMetaEnum(me);
                    me.UpdateAllName();
                    return me;
                }
                case IRMetaClassKind.Interface:
                {
                    var mi = new MetaClass(typeName, EClassDefineType.StructDefine);
                    mi.SetRefFromType(RefFromType.RefModule);
                    parent.AddMetaClass(mi);
                    mi.UpdateClassAllName();
                    return mi;
                }
                default: // Class
                {
                    var mc = new MetaClass(typeName, EClassDefineType.StructDefine);
                    mc.SetRefFromType(RefFromType.RefModule);

                    /* Template class: create MetaTemplate parameters before adding to MetaNode,
                     * so MetaNode.AddMetaClass registers it with the correct template count
                     * in m_MetaTemplateClassDict. */
                    if (cls.templateParameterCount > 0)
                    {
                        for (int ti = 0; ti < cls.templateParameterCount; ti++)
                        {
                            var tplName = ti == 0 ? "T" : "T" + ti.ToString();
                            var mt = new MetaTemplate(mc, tplName, CoreMetaClassManager.objectMetaClass, ECovariance.None);
                            mt.SetIndex(ti);
                            mc.metaTemplateList.Add(mt);
                        }
                    }

                    /* AddMetaClass creates the MetaNode and calls mc.SetMetaNode(node),
                     * so metaNode is set after this call. UpdateClassAllName must come
                     * after AddMetaClass because it traverses the metaNode parent chain. */
                    parent.AddMetaClass(mc);
                    mc.UpdateClassAllName();
                    return mc;
                }
            }
        }

        /// <summary>
        /// Phase B2: 从 IRMetaClass 反向构建 Meta 成员（成员变量 / 方法 / 枚举成员 / Data 成员）。
        /// 类型由 IRMetaType.ToMetaType 复原（需 Phase C LinkMetaOwners 已完成，typeOwner 已关联）。
        /// 基类 / 接口关系仍来自 SLClassPackage（IRMetaClass 不独立承载这两者）。
        /// </summary>
        private static void PopulateReferenceTypeMembersFromIR(MetaModule metaModule, IRMetaClass irmc,
            SLClassPackage cls, MetaBase metaBase, Dictionary<int, SLClassPackage> classLookup)
        {
            if (metaModule == null || irmc == null || cls == null || metaBase == null) return;

            /* Core replacement: clear inner-form members before repopulating with compiled data. */
            if (s_isCoreReplacement)
            {
                ClearExistingMembers(metaBase);
            }

            switch (irmc.metaClassKind)
            {
                case IRMetaClassKind.Data:
                {
                    var md = metaBase as MetaData;
                    if (md != null)
                    {
                        AddDataFieldsFromIR(md, irmc);
                    }
                    break;
                }
                case IRMetaClassKind.Enum:
                {
                    var me = metaBase as MetaEnum;
                    if (me != null)
                    {
                        AddEnumMembersFromIR(me, irmc);
                    }
                    break;
                }
                case IRMetaClassKind.Interface:
                case IRMetaClassKind.Class:
                default:
                {
                    var mc = metaBase as MetaClass;
                    if (mc == null) break;

                    AddClassFieldsFromIR(mc, irmc);
                    AddClassMethodsFromIR(mc, irmc);
                    SetBaseAndInterfacesFromPackage(mc, cls, metaModule, classLookup);
                    break;
                }
            }
        }

        /// <summary>
        /// Clears existing members (fields, methods, enum members) from a MetaBase.
        /// Used during Core replacement to wipe inner-form definitions before
        /// repopulating with compiled data.
        /// </summary>
        private static void ClearExistingMembers(MetaBase metaBase)
        {
            if (metaBase is MetaEnum me)
            {
                me.metaMemberEnumDict.Clear();
                me.metaMemberVariableDict.Clear();
            }
            else if (metaBase is MetaData md)
            {
                md.metaMemberDataDict.Clear();
                md.nonStaticVirtualMetaMemberFunctionList.Clear();
                md.staticMetaMemberFunctionList.Clear();
                md.fileCollectMetaMemberFunctionList.Clear();
            }
            else if (metaBase is MetaClass mc)
            {
                mc.metaMemberVariableDict.Clear();
                mc.fileCollectMetaMemberVariable.Clear();
                mc.metaMemberFunctionTemplateNodeDict.Clear();
                /* Clear all method lists - ref module JSON is the single source of truth.
                 * Don't move old methods to fileCollect, as HandleExtendMemberFunction
                 * would re-add them creating duplicates with the new JSON-imported methods. */
                mc.fileCollectMetaMemberFunctionList.Clear();
                mc.nonStaticVirtualMetaMemberFunctionList.Clear();
                mc.staticMetaMemberFunctionList.Clear();
            }
        }

        /* ---- 反向：IRMetaVariable -> Meta 成员 ---- */

        private static void AddClassFieldsFromIR(MetaClass mc, IRMetaClass irmc)
        {
            if (mc == null || irmc == null) return;
            foreach (var iv in irmc.localIRMetaVariableList)
            {
                AddClassFieldFromIR(mc, iv, irmc);
            }
            foreach (var iv in irmc.staticIRMetaVariableList)
            {
                AddClassFieldFromIR(mc, iv, irmc);
            }
        }

        private static void AddClassFieldFromIR(MetaClass mc, IRMetaVariable iv, IRMetaClass irmc)
        {
            if (iv == null || string.IsNullOrWhiteSpace(iv.shortName)) return;
            var mmv = new MetaMemberVariable(mc, iv.shortName);
            mmv.SetRefFromType(RefFromType.RefModule);
            mmv.SetIsStatic(iv.isStatic);
            mmv.SetIsConst(iv.isConst);
            var mt = IRMetaType.ToMetaType(iv.irMetaType, mc);
            mmv.SetMetaDefineType(mt);
            mmv.SetIsDefineMetaType(true);
            mmv.SetRealMetaType(new MetaType(mt));
            if (iv.index >= 0)
            {
                mmv.SetIndex(iv.index);
            }
            mc.AddMetaMemberVariable(mmv, isAddManager: false);
            /* 注册新建 MetaMemberVariable 的 hash 到 IRMetaClass 的 hash->index 字典，
             * 这样 CreateLoadVariable 的 GetMetaMemberVariableIndexByHashCode 才能命中。
             * IRMetaVariable（从 package 构建）的 m_Id 与 MetaMemberVariable.GetHashCode() 不同。 */
            irmc.AddMetaMemberVariableIndexBindHashCode(mmv.GetHashCode(), iv.index >= 0 ? iv.index : 0);
        }

        private static void AddDataFieldsFromIR(MetaData md, IRMetaClass irmc)
        {
            if (md == null || irmc == null) return;
            /* Data 成员在 IR 层同时出现在 local 与 static 列表（forward CreateMemberDataFromMetaData 行为），
             * 取 localIRMetaVariableList 即可得到每个成员一份。 */
            foreach (var iv in irmc.localIRMetaVariableList)
            {
                if (iv == null || string.IsNullOrWhiteSpace(iv.shortName)) continue;
                var fieldName = iv.shortName;
                var fieldIndex = iv.index >= 0 ? iv.index : md.metaMemberDataDict.Count;
                MetaType fieldType = IRMetaType.ToMetaType(iv.irMetaType, null);
                var mmd = MetaMemberData.CreateDeclared(md, fieldName, fieldIndex, fieldType, fieldType != null);
                md.AddMetaMemberData(mmd);
            }
        }

        private static void AddEnumMembersFromIR(MetaEnum me, IRMetaClass irmc)
        {
            if (me == null || irmc == null) return;
            /* 枚举成员在 IR 层位于 staticIRMetaVariableList。 */
            foreach (var iv in irmc.staticIRMetaVariableList)
            {
                if (iv == null || string.IsNullOrWhiteSpace(iv.shortName)) continue;

                /* "values" 是 Enum 自动生成的静态数组变量（Array<Member>），不是枚举成员。 */
                if (iv.shortName == "values")
                {
                    var mmv = new MetaMemberVariable(me, "values");
                    mmv.SetRefFromType(RefFromType.RefModule);
                    mmv.SetVariableFrom(MetaVariable.EVariableFrom.EnumMember);
                    mmv.SetIsStatic(true);
                    var mt = IRMetaType.ToMetaType(iv.irMetaType, null);
                    mmv.SetMetaDefineType(mt);
                    mmv.SetRealMetaType(new MetaType(mt));
                    mmv.SetIsDefineMetaType(true);
                    mmv.SetIndex(iv.index >= 0 ? iv.index : me.metaMemberVariableDict.Count);
                    if (!me.metaMemberVariableDict.ContainsKey("values"))
                    {
                        me.metaMemberVariableDict.Add("values", mmv);
                    }
                    irmc.AddMetaMemberVariableIndexBindHashCode(mmv.GetHashCode(), iv.index >= 0 ? iv.index : 0);
                    continue;
                }

                var memberName = iv.shortName;
                var memberIndex = iv.index >= 0 ? iv.index : me.metaMemberEnumDict.Count;
                var mme = new MetaMemberEnum(me, memberName, memberIndex);
                var memberMt = IRMetaType.ToMetaType(iv.irMetaType, null);
                mme.SetMetaDefineType(memberMt);
                mme.SetRealMetaType(new MetaType(memberMt));
                mme.SetIsDefineMetaType(true);
                me.metaMemberEnumDict.Add(mme.name, mme);
                me.metaMemberVariableDict.Add(mme.name, mme);
                /* 注册枚举成员 hash 到 IRMetaClass，CreateLoadVariable EnumMember 路径需要。 */
                irmc.AddMetaMemberVariableIndexBindHashCode(mme.GetHashCode(), memberIndex);
            }
        }

        /* ---- 反向：IRMethod -> MetaMemberFunction ---- */

        private static void AddClassMethodsFromIR(MetaClass mc, IRMetaClass irmc)
        {
            if (mc == null || irmc == null) return;

            if (irmc.nonStaticMethodList != null)
            {
                foreach (var irm in irmc.nonStaticMethodList)
                {
                    var mmf = BuildMetaMemberFunctionFromIR(mc, irm);
                    if (mmf != null)
                    {
                        mc.nonStaticVirtualMetaMemberFunctionList.Add(mmf);
                        mc.AddMetaMemberFunction(mmf);
                    }
                }
            }
            if (irmc.staticMethodList != null)
            {
                foreach (var irm in irmc.staticMethodList)
                {
                    var mmf = BuildMetaMemberFunctionFromIR(mc, irm);
                    if (mmf != null)
                    {
                        mc.staticMetaMemberFunctionList.Add(mmf);
                        mc.AddMetaMemberFunction(mmf);
                    }
                }
            }
            if (irmc.operatorMethodList != null)
            {
                foreach (var irm in irmc.operatorMethodList)
                {
                    var mmf = BuildMetaMemberFunctionFromIR(mc, irm);
                    if (mmf != null)
                    {
                        mc.nonStaticVirtualMetaMemberFunctionList.Add(mmf);
                        mc.AddMetaMemberFunction(mmf);
                    }
                }
            }
        }

        private static MetaMemberFunction BuildMetaMemberFunctionFromIR(MetaClass mc, IRMethod irm)
        {
            if (mc == null || irm == null || string.IsNullOrWhiteSpace(irm.onlyFunctionName)) return null;

            // 归属：若 declaringClassId 指向声明类（非当前类），owner 用声明类。
            // 继承来的方法仍加入当前类的虚表（AddClassMethodsFromIR 加入 mc 的列表），
            // 但 ownerMetaClass 是声明类（如 Core.Object.type 而非 Num.type）。
            MetaClass ownerClass = mc;
            if (irm.declaringClassId != 0 && irm.declaringClassId != mc.classId)
            {
                if (IRManager.instance.GetIRMetaClassById(irm.declaringClassId)?.typeOwner is MetaClass declMc)
                {
                    ownerClass = declMc;
                }
            }
            var mmf = new MetaMemberFunction(ownerClass, irm.onlyFunctionName);
            mmf.SetRefFromType(RefFromType.RefModule);
            mmf.SetIsStatic(irm.isStatic);
            mmf.SetIsFinal(irm.isFinal);
            mmf.SetIsAbstract(irm.isAbstract);
            mmf.SetIsOverrideFunction(irm.isOverrideFunction);
            mmf.SetIsOverrideInterface(irm.interfaceMethod);

            /* 返回值类型 */
            if (irm.methodReturnVariableList != null && irm.methodReturnVariableList.Count > 0)
            {
                var retVar = irm.methodReturnVariableList[0];
                if (retVar != null && retVar.irMetaType != null)
                {
                    var retType = IRMetaType.ToMetaType(retVar.irMetaType, mc);
                    mmf.SetDefineMetaType(retType);
                    mmf.SetRealMetaType(new MetaType(retType));
                    mmf.SetIsDefineMetaType(true);
                    if (mmf.returnMetaVariable != null)
                    {
                        var defMt = new MetaType(retType);
                        mmf.returnMetaVariable.SetMetaDefineType(defMt);
                        mmf.returnMetaVariable.SetRealMetaType(new MetaType(defMt));
                        mmf.returnMetaVariable.SetIsDefineMetaType(true);
                    }
                }
            }

            /* 参数：非静态方法第一个参数是隐式 this（IR 层保留），MetaCore 层用 thisMetaVariable 单独处理，跳过。 */
            if (irm.methodArgumentList != null)
            {
                int startIndex = irm.isStatic ? 0 : 1;
                for (int i = startIndex; i < irm.methodArgumentList.Count; i++)
                {
                    var arg = irm.methodArgumentList[i];
                    if (arg == null) continue;
                    var paramName = !string.IsNullOrWhiteSpace(arg.name) ? arg.name : "arg";
                    var mdp = new MetaDefineParam(paramName, mmf);
                    if (irm.isExtendParams && i == irm.methodArgumentList.Count - 1)
                    {
                        mdp.SetExtendParams();
                    }
                    if (arg.irMetaType != null)
                    {
                        var paramType = IRMetaType.ToMetaType(arg.irMetaType, mc);
                        mdp.metaVariable.SetMetaDefineType(paramType);
                        mdp.metaVariable.SetRealMetaType(new MetaType(paramType));
                        mdp.metaVariable.SetIsDefineMetaType(true);
                    }
                    mmf.AddMetaDefineParam(mdp);
                }
            }

            // ref module 函数需要通过 ParseDefineMetaType 设置 virtualFunctionName，
            // 否则 IR 阶段通过 virtualFunctionName 查找方法时会失败
            mmf.ParseDefineMetaType();

            // IRCall 虚调用按 virtualFunctionName 在 IRMetaClass 上查 IRMethod（GetIRNonStaticMethodIndexByMethod）。
            // 但 IRMethod.virtualFunctionName 由 ComputeVirtualFunctionName 从 package 的 typeDef.className 生成（去模块前缀，如 "Int32"），
            // 而 MetaMemberFunction.virtualFunctionName 由 UpdateVritualFunctionName 从 MetaType.ToString() 生成（全名，如 "Core.Int32"），
            // 两者对不上导致虚调用查不到方法。这里用 MetaMemberFunction 的 canonical 名同步回 IRMethod。
            irm.virtualFunctionName = mmf.virtualFunctionName;

            return mmf;
        }

        /* ---- 基类 / 接口（仍来自 SLClassPackage，IRMetaClass 不独立承载） ---- */

        private static void SetBaseAndInterfacesFromPackage(MetaClass mc, SLClassPackage cls,
            MetaModule metaModule, Dictionary<int, SLClassPackage> classLookup)
        {
            if (mc == null || cls == null) return;

            if (cls.baseClassId != 0 && classLookup.TryGetValue(cls.baseClassId, out var basePkg))
            {
                var baseFullName = !string.IsNullOrWhiteSpace(basePkg.fullName)
                    ? basePkg.fullName : basePkg.name;
                var baseType = ResolveTypeByName(metaModule, baseFullName);
                if (baseType != null)
                {
                    mc.SetExtendClass(baseType);
                }
                else
                {
                    Log.AddProjectLog(LID.MetaCoreAssertShowMessage, "not find base type");
                }
            }

            if (cls.implementsInterfaceIdList != null)
            {
                foreach (var ifaceId in cls.implementsInterfaceIdList)
                {
                    if (classLookup.TryGetValue(ifaceId, out var ifacePkg))
                    {
                        var ifaceFullName = !string.IsNullOrWhiteSpace(ifacePkg.fullName)
                            ? ifacePkg.fullName : ifacePkg.name;
                        var ifaceType = ResolveTypeByName(metaModule, ifaceFullName);
                        if (ifaceType != null)
                        {
                            mc.AddInterfaceClass(ifaceType);
                        }
                        else
                        {
                            Log.AddProjectLog(LID.MetaCoreAssertShowMessage, "not find base type");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a type by its full name within the module being loaded,
        /// falling back to CoreMetaClassManager for built-in types.
        /// </summary>
        private static MetaClass ResolveTypeByName(MetaModule metaModule, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;

            // Strip generic suffix (e.g. "IIterable<T>" -> "IIterable") and infer template arg count
            var baseName = fullName;
            int lt = baseName.IndexOf('<');
            if (lt > 0)
                baseName = baseName.Substring(0, lt);
            // Count comma-separated template args inside <...>
            int templateArgCount = 0;
            if (lt > 0)
            {
                int gt = fullName.IndexOf('>', lt + 1);
                if (gt > lt)
                {
                    var args = fullName.Substring(lt + 1, gt - lt - 1);
                    templateArgCount = string.IsNullOrWhiteSpace(args) ? 0 : args.Split(',').Length;
                }
            }
            int index = baseName.IndexOf(".");
            if( index != -1 )
            {
                baseName = baseName.Substring(index + 1, baseName.Length - index - 1 );
            }

            /* Check built-in core types first */
            var coreNode = CoreMetaClassManager.GetCoreMetaClass(baseName);
            if (coreNode != null)
            {
                var mc = coreNode.GetMetaClassByTemplateCount(templateArgCount);
                if (mc != null) return mc;
                // Fallback to non-generic
                mc = coreNode.GetMetaClassByTemplateCount(0);
                if (mc != null) return mc;
            }

            /* Search within the module's namespace tree */
            if (metaModule?.metaNode != null)
            {
                var parts = baseName.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var current = metaModule.metaNode;
                for (int i = 0; i < parts.Length; i++)
                {
                    current = current.GetChildrenMetaNodeByName(parts[i]);
                    if (current == null) break;
                }
                if (current != null && current.IsMetaClass())
                {
                    var mc = current.GetMetaClassByTemplateCount(templateArgCount);
                    if (mc != null) return mc;
                    mc = current.GetMetaClassByTemplateCount(0);
                    if (mc != null) return mc;
                }
            }

            return null;
        }

        #endregion

        #region Source module loading

        /// <summary>
        /// Loads a reference module from its source .jsonc file (struct tree only).
        /// This builds a MetaModule with namespace/class/data/enum declarations
        /// from the "struct" section of the referenced module's .jsonc config.
        /// No full compilation is performed - only the type structure is loaded
        /// so that import statements in the main project can resolve correctly.
        /// </summary>
        private static bool TryLoadSourceModule(ProjectConfig.ReferenceSection reference, string projectDir)
        {
            var resolvedDir = Path.IsPathRooted(reference.Path)
                ? Path.GetFullPath(reference.Path)
                : Path.GetFullPath(Path.Combine(projectDir ?? string.Empty, reference.Path));

            if (!Directory.Exists(resolvedDir))
            {
                return false;
            }

            /* Find the .jsonc file in the referenced directory */
            var jsoncFiles = Directory.GetFiles(resolvedDir, "*.jsonc", SearchOption.TopDirectoryOnly);
            if (jsoncFiles.Length == 0)
            {
                return false;
            }

            var jsoncPath = jsoncFiles[0];
            ProjectConfig refConfig;
            string jsoncText;
            try
            {
                jsoncText = File.ReadAllText(jsoncPath);
                refConfig = ProjectJsoncLoader.FromJsonc(jsoncText);
            }
            catch (Exception ex)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference source module .jsonc parse failed: {jsoncPath} {ex.Message}");
                return false;
            }

            /* Register the referenced module's system call declarations (declared in
             * its .jsonc "systemCalls" section) into the FrontEnd registry.
             * No-op when the section is absent. */
            int refSysCallCount = SystemMethodCallDeclarationRegistry.LoadFromJsonContent(jsoncText);
            if (refSysCallCount > 0)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference module registered {refSysCallCount} system calls. path={jsoncPath}");
            }

            /* Determine the module name (alias for import) */
            var alias = !string.IsNullOrWhiteSpace(reference.Name)
                ? reference.Name.Trim()
                : (!string.IsNullOrWhiteSpace(refConfig.Export.ModuleName)
                    ? refConfig.Export.ModuleName.Trim()
                    : Path.GetFileName(resolvedDir));

            if (string.IsNullOrWhiteSpace(alias))
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference module alias is empty: {jsoncPath}");
                return false;
            }

            /* Skip if module is already loaded (avoids duplicate loading). */
            var existingModule = ModuleManager.instance.GetMetaModuleByName(alias);
            if (existingModule != null)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference module '{alias}' is already loaded, skipping. path={jsoncPath}");
                Console.WriteLine($"[Reference] Module already loaded, skipping: name={alias}, path={jsoncPath}");
                return true;
            }

            /* Build MetaModule from the struct tree.
             * For "Core", reuse the existing coreModule (already populated by
             * CoreMetaClassManager.Init with inner-form types) instead of creating
             * a new empty module that would replace it via AddMetaMdoule. */
            MetaModule metaModule;
            bool isCoreRef = alias == "Core";
            if (isCoreRef)
            {
                metaModule = ModuleManager.instance.coreModule;
                BuildModuleTreeFromStructTree(metaModule, refConfig.StructTree);
            }
            else
            {
                metaModule = new MetaModule(alias);
                metaModule.SetRefFromType(RefFromType.Local);
                BuildModuleTreeFromStructTree(metaModule, refConfig.StructTree);
                ModuleManager.instance.AddMetaMdoule(metaModule);
            }

            // 将模块名注册为工程级类型别名，指向模块根节点。
            RegisterModuleAlias(alias, metaModule);

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module loaded (source): name={alias}, path={jsoncPath}, structCount={refConfig.StructTree.Children.Count}");
            Console.WriteLine($"[Reference] Module loaded (source): name={alias}, path={jsoncPath}, structs={refConfig.StructTree.Children.Count}");
            return true;
        }

        /// <summary>
        /// Builds a MetaModule's namespace/class tree from a ProjectConfig.StructTreeNode tree.
        /// Each node maps to: Namespace -> MetaNamespace, Class -> MetaClass,
        /// Data -> MetaData, Enum -> MetaEnum.
        /// </summary>
        private static void BuildModuleTreeFromStructTree(MetaModule metaModule, ProjectConfig.StructTreeNode root)
        {
            if (metaModule == null || root == null)
            {
                return;
            }

            foreach (var child in root.Children)
            {
                BuildStructNode(metaModule.metaNode, child);
            }
        }

        private static void BuildStructNode(MetaNode parent, ProjectConfig.StructTreeNode node)
        {
            if (parent == null || node == null || string.IsNullOrWhiteSpace(node.Name))
            {
                return;
            }

            switch (node.Type)
            {
                case ProjectConfig.StructTreeNode.NodeType.Namespace:
                    {
                        var existing = parent.GetChildrenMetaNodeByName(node.Name);
                        if (existing == null)
                        {
                            existing = parent.AddMetaNamespace(new MetaNamespace(node.Name));
                        }
                        foreach (var child in node.Children)
                        {
                            BuildStructNode(existing, child);
                        }
                        break;
                    }
                case ProjectConfig.StructTreeNode.NodeType.Class:
                    {
                        if (parent.GetChildrenMetaNodeByName(node.Name) == null)
                        {
                            var mc = new MetaClass(node.Name, EClassDefineType.StructDefine);
                            parent.AddMetaClass(mc);
                            mc.UpdateClassAllName();
                            ClassManager.instance.AddExportMetaClass(mc);
                        }
                        break;
                    }
                case ProjectConfig.StructTreeNode.NodeType.Data:
                    {
                        if (parent.GetChildrenMetaNodeByName(node.Name) == null)
                        {
                            var md = new MetaData(node.Name, false, false, false);
                            md.SetClassDefineType(EClassDefineType.StructDefine);
                            parent.AddMetaData(md);
                            ClassManager.instance.AddDefineMetaData(md);
                        }
                        break;
                    }
                case ProjectConfig.StructTreeNode.NodeType.Enum:
                    {
                        if (parent.GetChildrenMetaNodeByName(node.Name) == null)
                        {
                            var me = new MetaEnum(node.Name);
                            me.SetClassDefineType(EClassDefineType.StructDefine);
                            parent.AddMetaEnum(me);
                        }
                        break;
                    }
                case ProjectConfig.StructTreeNode.NodeType.Interface:
                    {
                        if (parent.GetChildrenMetaNodeByName(node.Name) == null)
                        {
                            var mi = new MetaClass(node.Name, EClassDefineType.StructDefine);
                            parent.AddMetaClass(mi);
                            mi.UpdateClassAllName();
                        }
                        break;
                    }
            }
        }

        #endregion

        private static string GetNamespace(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var idx = fullName.LastIndexOf('.');
            return idx > 0 ? fullName.Substring(0, idx) : string.Empty;
        }

        private static string GetShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var idx = fullName.LastIndexOf('.');
            return idx >= 0 && idx + 1 < fullName.Length ? fullName.Substring(idx + 1) : fullName;
        }
    }
}
