//****************************************************************************
//  File:      ProjectFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description: project class manager
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.VM.Runtime;
using SimpleLanguage.Parse;
using SimpleLanguage.IR;
using System.Diagnostics;

using SimpleLanguage.Compile;

namespace SimpleLanguage.Project
{
    public class ProjectClass
    {
        public static MetaClass projectEnter = null;
        public static MetaClass projectDll = null;
        public static MetaClass compile = null;

        public static MetaFunction s_MainFunction = null;
        public static MetaFunction s_TestFunction = null;
        public static MetaFunction s_LoadStartFunction = null;
        public static MetaFunction s_LoadEndFunction = null;
        public static MetaFunction s_CompileBeforeFunction = null;
        public static MetaFunction s_CompileAfterFunction = null;
        public static void ParseCompileClass()
        {
            FileMetaClass fmc = ProjectCompile.projectFileMeta.GetFileMetaClassByName("Compile");

            if (fmc == null) return;

            ClassManager.instance.AddClass(fmc);
            
            compile = fmc.metaClass;
            if (compile == null) return;

            //compile.Parse();
            compile.ParseDefineComplete();
            var flist = compile.staticMetaMemberFunctionList;
            for( int i = 0; i < flist.Count; i++ )
            {
                flist[i].ParseStatements();
            }
            //s_CompileBeforeFunction = compile.GetMetaDefineGetSetMemberFunctionByName("CompileBefore",false,false);
            //s_CompileAfterFunction = compile.GetMetaDefineGetSetMemberFunctionByName("CompileAfter", false, false);

        }
        public static void RunTest()
        {
            MetaClass project = ClassManager.instance.GetClassByName("Project");
            if (project == null)
            {
                Debug.Write("Error project!!");
                return;
            }
            MetaMemberFunction mmf = project.GetFirstMetaMemberFunctionByName("Test");
            if (mmf == null)
            {
                Debug.Write("Error project.Main函数!!");
                return;
            }
            //var irmethod = IRManager.instance.GetIRMethod(mmf.allName);
            //InnerCLRRuntimeVM.Init();
            //InnerCLRRuntimeVM.RunIRMethod(irmethod);
        }
        public static void RunMain()
        {
            MetaClass projectEntoer = ClassManager.instance.GetClassByName("S.Project", 0);
            if (projectEntoer == null)
            {
                Debug.Write("Error 没有找到Project!!");
                return;
            }
            MetaMemberFunction mmf = projectEntoer.GetFirstMetaMemberFunctionByName("Main");
            if (mmf == null)
            {
                Debug.Write("Error 没有找到Project.Main函数!!");
                return;
            }
            var irmethod = IRManager.instance.GetIRMethod(mmf.functionAllName);
            InnerCLRRuntimeVM.Init();
            InnerCLRRuntimeVM.RunIRMethod( null, irmethod);
        }
        // Build MetaNode / MetaNamespace tree from StructTreeNode description.
        // parentRoot: existing MetaNode root (通常是 ModuleManager.instance.selfModule.metaNode)
        // node: StructTreeNode from ProjectConfig.StructTree (Root/Namespace/Class)
        public static void AddDefineNamespace(MetaNode parentRoot, ProjectConfig.StructTreeNode node, bool isAddCurrent = true)
        {
            if (parentRoot == null || node == null)
                return;

            // Root 节点只作为逻辑起点，不对应具体 namespace/class，本身不创建 MetaNamespace/MetaClass。
            if (node.Type == ProjectConfig.StructTreeNode.NodeType.Root)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parentRoot, node.Children[i], true);
                }
                return;
            }

            MetaNode parMS = null;

            if (node != null)
            {
                if (isAddCurrent)
                {
                    // 尝试在当前父节点下查找同名子节点
                    var cfindNode = parentRoot.GetChildrenMetaNodeByName(node.Name);
                    if (cfindNode == null)
                    {
                        // 不存在同名节点，按 StructTreeNode 类型创建新的 MetaClass / MetaNamespace
                        if (node.Type == ProjectConfig.StructTreeNode.NodeType.Class)
                        {
                            // class: 先尝试从 CoreMetaClassManager 获取内置类，否则创建普通 StructDefine 类
                            var gcmc = CoreMetaClassManager.GetCoreMetaClass(node.Name);
                            if (gcmc != null)
                            {
                                parMS = parentRoot.AddMetaClass(gcmc.GetMetaClassByTemplateCount(0));
                            }
                            else
                            {
                                var nodens = new MetaClass(node.Name, EClassDefineType.StructDefine);
                                parMS = parentRoot.AddMetaClass(nodens);
                            }
                        }
                        else
                        {
                            // namespace / 其它类型一律按命名空间处理
                            var nodeNs = new MetaNamespace(node.Name);
                            parMS = parentRoot.AddMetaNamespace(nodeNs);
                        }
                    }
                    else
                    {
                        // 已有同名子节点，要求其必须是命名空间节点
                        if (!cfindNode.isMetaNamespace)
                        {
                            Log.AddInStructMeta(EError.None, "Error 解析namespace添加命名空间节点时，发现已有定义类!!");
                            return;
                        }
                        // 复用已有命名空间节点
                        parMS = parentRoot.AddMetaNamespace(cfindNode.metaNamespace);
                    }
                }
                else
                {
                    parMS = parentRoot;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parMS, node.Children[i]);
                }
            }
        }
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            // 使用 TOML 基于的 ProjectConfig 填充编译文件列表
            var cfg = ProjectManager.currentProject?.Config;
            if (cfg == null)
                return;

            AddDefineNamespace(ModuleManager.instance.selfModule.metaNode, cfg.StructTree, false);

            var fileList = cfg.CompileFiles.Files;
            var filter = cfg.CompileFilter;

            System.Diagnostics.Debug.WriteLine($"[Project] compileFiles count in config = {fileList.Count}");
 
            for (int i = 0; i < fileList.Count; i++)
            {
                var fld = fileList[i];

                if (IsCanAddFile(filter, fld))
                {
                    ProjectCompile.AddFileParse(fld.Path);
                }
            }

            if (s_CompileBeforeFunction!=null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }

        }
        public static bool IsCanAddFile(ProjectConfig.CompileFilterSection cfd, ProjectConfig.CompileFileItem fileData )
        {
            if (cfd == null) return true;

            // group 过滤
            if (!cfd.IsAllGroup)
            {
                if (cfd.Groups.Count > 0 && !cfd.Groups.Contains(fileData.Group))
                    return false;
            }

            // tag 过滤
            if (!cfd.IsAllTag)
            {
                if (cfd.Tags.Count > 0 && !cfd.Tags.Contains(fileData.Tag))
                    return false;
            }

            // ignore 标志
            if (fileData.Ignore)
                return false;

            return true;
        }
        public static void ProjectCompileAfter()
        {
            if (s_CompileBeforeFunction != null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }
        }
    }
}
