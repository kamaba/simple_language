using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.IO;
using System.Linq;

namespace SimpleLanguage.Project
{
    /// <summary>
    /// Loads referenced modules declared in the "references" section of a project's .jsonc config.
    /// Each reference has: path (relative or absolute), uuid (optional), name (the import alias).
    ///
    /// Loading strategy (tried in order):
    ///   1. Compiled package  (.package.json / .module.json) at or near the path
    ///   2. Source module      (directory containing a .jsonc with struct tree)
    ///   3. Export output      (look in the referenced module's export outputDir)
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

            /* --- Strategy 1: Try compiled package (.package.json / .module.json) --- */
            var packagePath = ResolveReferenceModulePath(reference.Path, projectDir);
            if (!string.IsNullOrWhiteSpace(packagePath) && File.Exists(packagePath))
            {
                if (TryLoadCompiledPackage(reference, packagePath))
                {
                    return;
                }
            }

            /* --- Strategy 2: Try source module (.jsonc with struct tree) --- */
            if (TryLoadSourceModule(reference, projectDir))
            {
                return;
            }

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module not found or could not be loaded: path={reference.Path}, name={reference.Name}");
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

            if (!ValidateUuid(reference, package, modulePath))
            {
                return false;
            }

            var alias = ResolveModuleName(reference, package, modulePath);

            var metaModule = new MetaModule(alias);
            metaModule.SetRefFromType(RefFromType.Local);
            BuildModuleTree(metaModule, package);
            ModuleManager.instance.AddMetaMdoule(metaModule);

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module loaded (compiled): name={alias}, path={modulePath}");
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
                    AddReferenceType(metaModule, package.classList[i]);
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

        private static void AddReferenceType(MetaModule metaModule, SLClassPackage cls)
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
                    var md = new MetaData(typeName, false, false, cls.isDynamic);
                    md.SetAllName(fullName);
                    md.SetClassDefineType(EClassDefineType.StructDefine);
                    parent.AddMetaData(md);
                    ClassManager.instance.AddDefineMetaData(md);
                    break;
                case IRMetaClassKind.Enum:
                    var me = new MetaEnum(typeName);
                    me.SetClassDefineType(EClassDefineType.StructDefine);
                    parent.AddMetaEnum(me);
                    break;
                default:
                    var mc = new MetaClass(typeName, EClassDefineType.StructDefine);
                    parent.AddMetaClass(mc);
                    mc.UpdateClassAllName();
                    ClassManager.instance.AddExportMetaClass(mc);
                    break;
            }
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

            /* Build MetaModule from the struct tree */
            var metaModule = new MetaModule(alias);
            metaModule.SetRefFromType(RefFromType.Local);
            BuildModuleTreeFromStructTree(metaModule, refConfig.StructTree);
            ModuleManager.instance.AddMetaMdoule(metaModule);

            Log.AddProjectLog(LID.ShowExtendMessage,
                $"Reference module loaded (source): name={alias}, path={jsoncPath}, structCount={refConfig.StructTree.Children.Count}");
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
