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

            var modulePath = ResolveReferenceModulePath(reference.Path, projectDir);
            if (string.IsNullOrWhiteSpace(modulePath) || !File.Exists(modulePath))
            {
                Log.AddProjectLog(LID.ShowExtendMessage, "Reference module not found: " + reference.Path);
                return;
            }

            SLModulePackage package;
            try
            {
                package = SLModulePackageWriter.ReadWithoutInstructionCode(modulePath);
            }
            catch (Exception ex)
            {
                Log.AddProjectLog(LID.ShowExtendMessage, "Reference module read failed: " + modulePath + " " + ex.Message);
                return;
            }

            if (!ValidateUuid(reference, package, modulePath))
            {
                return;
            }

            var alias = !string.IsNullOrWhiteSpace(reference.Name)
                ? reference.Name.Trim()
                : (!string.IsNullOrWhiteSpace(package.moduleName) ? package.moduleName.Trim() : Path.GetFileNameWithoutExtension(modulePath));
            if (string.IsNullOrWhiteSpace(alias))
            {
                Log.AddProjectLog(LID.ShowExtendMessage, "Reference module alias is empty: " + modulePath);
                return;
            }

            var metaModule = new MetaModule(alias);
            metaModule.SetRefFromType(RefFromType.Local);
            BuildModuleTree(metaModule, package);
            ModuleManager.instance.AddMetaMdoule(metaModule);
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

            Log.AddProjectLog(LID.ShowExtendMessage, $"Reference module uuid mismatch: {modulePath}, expected={expected}, actual={actual}");
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
