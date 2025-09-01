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
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.IR;
using System.Diagnostics;
using SimpleLanguage.Core.SelfMeta;

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
        public static void AddDefineNamespace( MetaNode parentRoot, DefineStruct dns, bool isAddCurrent = true )
        {
            if (parentRoot == null) return;

            MetaNode parMS = null;
            if ( dns != null )
            {
                MetaNamespace nodeNS = null;
                if (isAddCurrent)
                {
                    var cfindNode = parentRoot.GetChildrenMetaNodeByName(dns.name);
                    if (cfindNode == null)
                    {
                        if( dns.type == DefineStruct.EDefineStructType.Class )
                        {
                            var gcmc = CoreMetaClassManager.GetCoreMetaClass(dns.name);
                            if( gcmc != null )
                            {
                                parMS = parentRoot.AddMetaClass(gcmc.GetMetaClassByTemplateCount(0));
                            }
                            else
                            {
                                var nodens = new MetaClass(dns.name, EClassDefineType.StructDefine);
                                parMS = parentRoot.AddMetaClass(nodens);
                            }                         
                        }
                        else
                        {
                            nodeNS = new MetaNamespace(dns.name);
                            parMS = parentRoot.AddMetaNamespace(nodeNS);
                        }
                    }
                    else
                    {
                        if (!(cfindNode.isMetaNamespace))
                        {
                            Log.AddInStructMeta( EError.None, "Error 解析namespace添加命名空间节点时，发现已有定义类!!");
                            return;
                        }
                        nodeNS = cfindNode.metaNamespace;
                        parMS = parentRoot.AddMetaNamespace(nodeNS);
                    }
                }
                else
                {
                    parMS = parentRoot;
                }
                for( int i = 0; i < dns.childDefineStruct.Count; i++ )
                {
                    AddDefineNamespace(parMS, dns.childDefineStruct[i] );
                }
            }
        }
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            ProjectData data = ProjectManager.data;
            AddDefineNamespace( ModuleManager.instance.selfModule.metaNode, data.namespaceRoot, false );

            var fileList = data.compileFileData.compileFileDataUnitList;
            var filter = data.compileFilterData;

            for (int i = 0; i < fileList.Count; i++)
            {
                var fld = fileList[i];

                if (IsCanAddFile(filter, fld))
                {
                    ProjectCompile.AddFileParse(fld.path);
                }
            }

            if (s_CompileBeforeFunction!=null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }

        }
        public static bool IsCanAddFile(CompileFilterData cfd, CompileFileData.CompileFileDataUnit fileData )
        {
            if (cfd == null) return true;
            if (!cfd.IsIncludeInGroup(fileData.group))
            {
                return false;
            }
            if (!cfd.IsIncludeInTag(fileData.tag))
            {
                return false;
            }
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
