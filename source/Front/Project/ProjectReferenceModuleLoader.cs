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

            /* --- Strategy 1: Try source module (.jsonc with struct tree) --- */
            if (TryLoadSourceModule(reference, projectDir))
            {
                return;
            }

            /* --- Strategy 2: Fallback to compiled package (.package.json / .module.json) --- */
            var packagePath = ResolveReferenceModulePath(reference.Path, projectDir);
            if (!string.IsNullOrWhiteSpace(packagePath) && File.Exists(packagePath))
            {
                if (TryLoadCompiledPackage(reference, packagePath))
                {
                    return;
                }
            }

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module not found or could not be loaded: path={reference.Path}, name={reference.Name}");
            Console.WriteLine($"[Reference] Failed to load module: name={reference.Name}, path={reference.Path}");
            return;
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

            var alias = ResolveModuleName(reference, package, modulePath);

            /* Skip if module is already loaded (e.g. Core was already loaded as a reference).
             * When Core is still in inner form (C# layer via CoreMetaClassManager.Init),
             * "Core" is NOT in m_AllMetaModuleDict, so GetMetaModuleByName returns null
             * and we proceed with loading (replacing the inner form with the compiled package). */
            var existingModule = ModuleManager.instance.GetMetaModuleByName(package.moduleName);
            if (existingModule != null)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference module '{package.moduleName}' is already loaded, skipping. path={modulePath}");
                Console.WriteLine($"[Reference] Module already loaded, skipping: name={package.moduleName}, path={modulePath}");
                return true;
            }

            var metaModule = new MetaModule(package.moduleName);
            metaModule.SetRefFromType(RefFromType.Local);
            BuildModuleTree(metaModule, package);
            ModuleManager.instance.AddMetaMdoule(metaModule);

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
                return reference.Name.Trim();
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
                    BuildAssemblyModuleTree(metaModule, package.moduleList[i]);
                }
                return;
            }

            BuildAssemblyModuleTree(metaModule, new SLAssemblyPackage
            {
                moduleName = package.moduleName,
                uuid = package.uuid,
                namespaceList = package.namespaceList,
                classList = package.classList,
                globalStaticVariableList = package.globalStaticVariableList,
                methodList = package.methodList,
            });
        }

        private static void BuildAssemblyModuleTree(MetaModule metaModule, SLAssemblyPackage package)
        {
            if (metaModule == null || package == null)
            {
                return;
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

            /* Build class lookup: id -> SLClassPackage for interface/inheritance resolution */
            var classLookup = new Dictionary<int, SLClassPackage>();
            if (package.classList != null)
            {
                foreach (var c in package.classList)
                {
                    if (c != null) classLookup[c.id] = c;
                }
            }

            if (package.namespaceList != null)
            {
                for (int i = 0; i < package.namespaceList.Count; i++)
                {
                    EnsureNamespacePath(metaModule.metaNode, package.namespaceList[i]?.fullName);
                }
            }

            if (package.classList != null)
            {
                for (int i = 0; i < package.classList.Count; i++)
                {
                    AddReferenceType(metaModule, package.classList[i], methodLookup, classLookup);
                }
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
        /// Creates a MetaClass/MetaData/MetaEnum from an exported SLClassPackage,
        /// restoring: class kind, member variables (with defineMetaType/realMetaType),
        /// enum members, member function signatures, base class inheritance,
        /// template definitions, and interface relationships.
        /// Function instruction bodies and expressions are NOT restored (only signatures/types).
        /// </summary>
        private static void AddReferenceType(MetaModule metaModule, SLClassPackage cls,
            Dictionary<string, SLMethodPackage> methodLookup,
            Dictionary<int, SLClassPackage> classLookup)
        {
            if (metaModule == null || cls == null)
            {
                return;
            }

            var fullName = !string.IsNullOrWhiteSpace(cls.fullName) ? cls.fullName : cls.name;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return;
            }

            var nsName = GetNamespace(fullName);
            var typeName = !string.IsNullOrWhiteSpace(cls.name) ? cls.name : GetShortName(fullName);
            var parent = EnsureNamespacePath(metaModule.metaNode, nsName);
            if (parent == null || parent.GetChildrenMetaNodeByName(typeName) != null)
            {
                return;
            }

            switch ((IRMetaClassKind)cls.metaClassKind)
            {
                case IRMetaClassKind.Data:
                {
                    var md = new MetaData(typeName, false, false, cls.isDynamic);
                    md.SetAllName(fullName);
                    md.SetClassDefineType(EClassDefineType.StructDefine);
                    AddFieldsToData(md, cls.fieldList, metaModule);
                    parent.AddMetaData(md);
                    ClassManager.instance.AddDefineMetaData(md);
                    break;
                }
                case IRMetaClassKind.Enum:
                {
                    var me = new MetaEnum(typeName);
                    me.SetClassDefineType(EClassDefineType.StructDefine);
                    AddEnumMembers(me, cls.fieldList, metaModule);
                    parent.AddMetaEnum(me);
                    me.UpdateAllName();
                    break;
                }
                case IRMetaClassKind.Interface:
                {
                    var mi = new MetaClass(typeName, EClassDefineType.StructDefine);
                    parent.AddMetaClass(mi);
                    mi.UpdateClassAllName();
                    AddFieldsToClass(mi, cls.fieldList, metaModule);
                    AddMethodsToClass(mi, cls, methodLookup);
                    break;
                }
                default: // Class
                {
                    var mc = new MetaClass(typeName, EClassDefineType.StructDefine);
                    parent.AddMetaClass(mc);
                    mc.UpdateClassAllName();

                    /* Template class: log template count for diagnostics */
                    if (cls.templateParameterCount > 0)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage,
                            $"Reference class '{fullName}' is a template class with {cls.templateParameterCount} type parameters (loaded as struct define).");
                    }

                    /* Member variables */
                    AddFieldsToClass(mc, cls.fieldList, metaModule);

                    /* Member functions (signatures only, no instruction bodies) */
                    AddMethodsToClass(mc, cls, methodLookup);

                    /* Base class relationship */
                    if (cls.baseClassId != 0)
                    {
                        if (classLookup.TryGetValue(cls.baseClassId, out var basePkg))
                        {
                            var baseFullName = !string.IsNullOrWhiteSpace(basePkg.fullName)
                                ? basePkg.fullName : basePkg.name;
                            var baseType = ResolveTypeByName(metaModule, baseFullName);
                            if (baseType != null)
                            {
                                mc.SetExtendClass(baseType);
                            }
                        }
                    }

                    /* Interface relationships */
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
                            }
                        }
                    }

                    ClassManager.instance.AddExportMetaClass(mc);
                    break;
                }
            }
        }

        private static void AddFieldsToClass(MetaClass mc, List<SLFieldPackage> fieldList, MetaModule metaModule)
        {
            if (mc == null || fieldList == null) return;

            foreach (var field in fieldList)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.name)) continue;

                var mmv = new MetaMemberVariable(mc, field.name);
                ApplyFieldTypeInfo(mmv, metaModule, field);
                mc.AddMetaMemberVariable(mmv);
            }
        }

        private static void AddFieldsToData(MetaData md, List<SLFieldPackage> fieldList, MetaModule metaModule)
        {
            if (md == null || fieldList == null) return;

            foreach (var field in fieldList)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.name)) continue;

                var mmv = new MetaMemberVariable(md, field.name);
                ApplyFieldTypeInfo(mmv, metaModule, field);
                md.AddMetaMemberVariable(mmv);
            }
        }

        /// <summary>
        /// Restores enum members from the exported fieldList.
        /// Each field becomes a MetaMemberVariable added to the enum's member dictionary.
        /// </summary>
        private static void AddEnumMembers(MetaEnum me, List<SLFieldPackage> fieldList, MetaModule metaModule)
        {
            if (me == null || fieldList == null) return;

            foreach (var field in fieldList)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.name)) continue;

                var mmv = new MetaMemberVariable(me, field.name);
                ApplyFieldTypeInfo(mmv, metaModule, field);
                // Enum members are const by nature
                mmv.SetIsConst(true);
                mmv.SetIsStatic(false);
                mmv.SetVariableFrom(MetaVariable.EVariableFrom.EnumMember);
                me.metaMemberVariableDict.Add(mmv.name, mmv);
            }
        }

        private static void AddMethodsToClass(MetaClass mc, SLClassPackage cls,
            Dictionary<string, SLMethodPackage> methodLookup)
        {
            if (mc == null || cls == null) return;

            /* Non-static (instance) methods */
            if (cls.nonStaticMethodList != null)
            {
                foreach (var meta in cls.nonStaticMethodList)
                {
                    AddMethodToClass(mc, meta, methodLookup, isStatic: false);
                }
            }

            /* Static methods */
            if (cls.staticMethodList != null)
            {
                foreach (var meta in cls.staticMethodList)
                {
                    AddMethodToClass(mc, meta, methodLookup, isStatic: true);
                }
            }

            /* Operator methods */
            if (cls.operatorMethodList != null)
            {
                foreach (var meta in cls.operatorMethodList)
                {
                    AddMethodToClass(mc, meta, methodLookup, isStatic: false);
                }
            }
        }

        private static void AddMethodToClass(MetaClass mc, SLMethodMeta meta,
            Dictionary<string, SLMethodPackage> methodLookup, bool isStatic)
        {
            if (mc == null || meta == null || string.IsNullOrWhiteSpace(meta.name)) return;

            var mmf = new MetaMemberFunction(mc, meta.name);
            mmf.SetIsStatic(isStatic);

            mc.AddMetaMemberFunction(mmf);
        }

        /// <summary>
        /// Resolves a type by its full name within the module being loaded,
        /// falling back to CoreMetaClassManager for built-in types.
        /// </summary>
        private static MetaClass ResolveTypeByName(MetaModule metaModule, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;

            /* Check built-in core types first */
            var coreNode = CoreMetaClassManager.GetCoreMetaClass(fullName);
            if (coreNode != null)
            {
                return coreNode.GetMetaClassByTemplateCount(0);
            }

            /* Search within the module's namespace tree */
            if (metaModule?.metaNode != null)
            {
                var parts = fullName.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var current = metaModule.metaNode;
                for (int i = 0; i < parts.Length; i++)
                {
                    current = current.GetChildrenMetaNodeByName(parts[i]);
                    if (current == null) break;
                }
                if (current != null && current.IsMetaClass())
                {
                    return current.GetMetaClassByTemplateCount(0);
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
            try
            {
                var jsoncText = File.ReadAllText(jsoncPath);
                refConfig = ProjectJsoncLoader.FromJsonc(jsoncText);
            }
            catch (Exception ex)
            {
                Log.AddProjectLog(LID.ShowExtendMessage,
                    $"Reference source module .jsonc parse failed: {jsoncPath} {ex.Message}");
                return false;
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

            /* Build MetaModule from the struct tree */
            var metaModule = new MetaModule(alias);
            metaModule.SetRefFromType(RefFromType.Local);
            BuildModuleTreeFromStructTree(metaModule, refConfig.StructTree);
            ModuleManager.instance.AddMetaMdoule(metaModule);

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

        #region Type resolution helpers

        /// <summary>
        /// Resolves a MetaNode by its full name (dot-separated) within the module's namespace tree.
        /// </summary>
        private static MetaNode ResolveMetaNodeByFullName(MetaNode root, string fullName)
        {
            if (root == null || string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var parts = fullName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                current = current.GetChildrenMetaNodeByName(parts[i]);
                if (current == null) break;
            }
            return current;
        }

        /// <summary>
        /// Converts an <see cref="SLRuntimeDefTypePackage"/> (exported type payload) back to a <see cref="MetaType"/>.
        /// Resolves built-in core types first, then searches the module's namespace tree.
        /// Template parameters (isTemplate=true) fall back to object since source context is unavailable.
        /// Generic types with runtimeDefTypeList are reconstructed with their template arguments.
        /// </summary>
        private static MetaType ResolveRuntimeDefType(MetaModule metaModule, SLRuntimeDefTypePackage typeDef)
        {
            if (typeDef == null)
            {
                return new MetaType(CoreMetaClassManager.objectMetaClass);
            }

            // Template parameter: cannot resolve without source context
            if (typeDef.isTemplate)
            {
                return new MetaType(CoreMetaClassManager.objectMetaClass);
            }

            var className = typeDef.className ?? string.Empty;
            if (string.IsNullOrWhiteSpace(className))
            {
                return new MetaType(CoreMetaClassManager.objectMetaClass);
            }

            // Try built-in core types first
            var coreNode = CoreMetaClassManager.GetCoreMetaClass(className);
            if (coreNode != null)
            {
                var mc = coreNode.GetMetaClassByTemplateCount(0);
                if (mc != null)
                {
                    return BuildMetaTypeWithTemplateArgs(mc, metaModule, typeDef);
                }
                if (coreNode.metaData != null)
                {
                    return new MetaType(coreNode.metaData);
                }
                if (coreNode.metaEnum != null)
                {
                    return new MetaType(coreNode.metaEnum);
                }
            }

            // Search within the module's namespace tree
            if (metaModule?.metaNode != null)
            {
                var node = ResolveMetaNodeByFullName(metaModule.metaNode, className);
                if (node != null)
                {
                    if (node.metaData != null)
                    {
                        return new MetaType(node.metaData);
                    }
                    if (node.metaEnum != null)
                    {
                        return new MetaType(node.metaEnum);
                    }
                    var mc = node.GetMetaClassByTemplateCount(0);
                    if (mc != null)
                    {
                        return BuildMetaTypeWithTemplateArgs(mc, metaModule, typeDef);
                    }
                }
            }

            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }

        /// <summary>
        /// Builds a MetaType from a MetaClass, attaching template type arguments if present.
        /// </summary>
        private static MetaType BuildMetaTypeWithTemplateArgs(MetaClass mc, MetaModule metaModule, SLRuntimeDefTypePackage typeDef)
        {
            if (typeDef.runtimeDefTypeList != null && typeDef.runtimeDefTypeList.Count > 0)
            {
                var templateArgs = new List<MetaType>();
                foreach (var child in typeDef.runtimeDefTypeList)
                {
                    templateArgs.Add(ResolveRuntimeDefType(metaModule, child));
                }
                return new MetaType(mc, templateArgs);
            }
            return new MetaType(mc);
        }

        /// <summary>
        /// Restores field type info (defineMetaType/realMetaType), isStatic, isConst from an SLFieldPackage.
        /// </summary>
        private static void ApplyFieldTypeInfo(MetaMemberVariable mmv, MetaModule metaModule, SLFieldPackage field)
        {
            // flags bits: 1=private, 2=public, 4=export, 8=protected, 16=const, 32=static
            mmv.SetIsStatic((field.flags & 32) != 0);
            mmv.SetIsConst((field.flags & 16) != 0);

            if (field.typeDef != null)
            {
                var mt = ResolveRuntimeDefType(metaModule, field.typeDef);
                mmv.SetMetaDefineType(mt);
                mmv.SetIsDefineMetaType(true);
                mmv.SetRealMetaType(new MetaType(mt));
            }

            if (field.index >= 0)
            {
                mmv.SetIndex(field.index);
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
