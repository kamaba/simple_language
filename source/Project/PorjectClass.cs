//****************************************************************************
//  File:      ProjectFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.VM.Runtime;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Compile.CoreFileMeta;

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
            FileMetaClass fmc = ProjectCompile.file.GetFileMetaClassByName("Compile");

            if (fmc == null) return;

            ClassManager.instance.AddClass(fmc);

            compile = fmc.metaClass;
            if (compile == null) return;

            compile.Parse();
            compile.ParseDefineComplete();
            var flist = compile.GetMemberFunctionList();
            for( int i = 0; i < flist.Count; i++ )
            {
                flist[i].ParseStatements();
            }
            s_CompileBeforeFunction = compile.GetMetaDefineGetSetMemberFunctionByName("CompileBefore",false,false);
            s_CompileAfterFunction = compile.GetMetaDefineGetSetMemberFunctionByName("CompileAfter", false, false);

            s_CompileBeforeFunction.TranslateIR();
            s_CompileAfterFunction.TranslateIR();
        }
        public static void ParseEnter()
        {
            FileMetaClass fmc = ProjectCompile.file.GetFileMetaClassByName("ProjectEnter");

            if (fmc == null) return;
            ClassManager.instance.AddClass(fmc);

            projectEnter = fmc.metaClass;
            if (projectEnter == null) return;

            projectEnter.Parse();
            projectEnter.ParseDefineComplete();
            var flist = projectEnter.GetMemberFunctionList();
            for (int i = 0; i < flist.Count; i++)
            {
                flist[i].ParseStatements();
            }
            s_MainFunction = projectEnter.GetMetaDefineGetSetMemberFunctionByName("Main", false, false);
            s_TestFunction = projectEnter.GetMetaDefineGetSetMemberFunctionByName("Test", false, false);

            s_MainFunction.TranslateIR();
            s_TestFunction.TranslateIR();
        }
        public static void ParseDll()
        {
            FileMetaClass fmc = ProjectCompile.file.GetFileMetaClassByName("ProjectDll");

            if (fmc == null) return;

            ClassManager.instance.AddClass(fmc);

            projectDll = fmc.metaClass;
            if (projectDll == null) return;

            projectDll.Parse();
            projectDll.ParseDefineComplete();
            var flist = projectDll.GetMemberFunctionList();
            for (int i = 0; i < flist.Count; i++)
            {
                flist[i].ParseStatements();
            }
            s_LoadStartFunction = projectDll.GetMetaDefineGetSetMemberFunctionByName("LoadStart", false, false);
            s_LoadEndFunction = projectDll.GetMetaDefineGetSetMemberFunctionByName("LoadEnd", false, false);

            s_LoadStartFunction.TranslateIR();
            s_LoadEndFunction.TranslateIR();
        }
        public static void RunTest()
        {
            MetaClass projectEntoer = ClassManager.instance.GetClassByName("ProjectEnter");
            if (projectEntoer == null)
            {
                Console.WriteLine("Error 没有找到ProjectEnter!!");
                return;
            }
            MetaMemberFunction mmf = projectEntoer.GetMetaMemberFunctionByAllName("Test");
            if (mmf == null)
            {
                Console.WriteLine("Error 没有找到ProjectEnter.Main函数!!");
                return;
            }
            InnerCLRRuntimeVM.Init();
            InnerCLRRuntimeVM.RunIRMethod(mmf.irMethod);
        }
        public static void RunMain()
        {
            MetaClass projectEntoer = ClassManager.instance.GetClassByName("ProjectEnter");
            if (projectEntoer == null)
            {
                Console.WriteLine("Error 没有找到ProjectEnter!!");
                return;
            }
            MetaMemberFunction mmf = projectEntoer.GetMetaMemberFunctionByAllName("Main");
            if (mmf == null)
            {
                Console.WriteLine("Error 没有找到ProjectEnter.Main函数!!");
                return;
            }
            InnerCLRRuntimeVM.Init();
            InnerCLRRuntimeVM.RunIRMethod(mmf.irMethod);
        }
        public static void AddDefineNamespace( MetaBase parentRoot, DefineNamespace dns, bool isAddCurrent = true )
        {
            if (parentRoot == null) return;

            MetaBase parMS = null;
            if ( dns != null )
            {
                MetaNamespace nodeNS = null;
                if (isAddCurrent)
                {
                    var cfindNode = parentRoot.GetChildrenMetaBaseByName(dns.spaceName);
                    if (cfindNode == null)
                    {
                        nodeNS = new MetaNamespace(dns.spaceName);
                    }
                    else
                    {
                        if (!(nodeNS is MetaNamespace))
                        {
                            Console.Write("Error 解析namespace添加命名空间节点时，发现已有定义类!!");
                            return;
                        }
                        nodeNS = cfindNode as MetaNamespace;
                    }
                    if (parentRoot is MetaModule)
                    {
                        (parentRoot as MetaModule).AddMetaNamespace(nodeNS);
                    }
                    else if (parentRoot is MetaNamespace)
                    {
                        (parentRoot as MetaNamespace).AddMetaNamespace(nodeNS);
                    }
                    parMS = nodeNS;
                }
                else
                {
                    parMS = parentRoot;
                }
                for( int i = 0; i < dns.childDefineNamespace.Count; i++ )
                {
                    AddDefineNamespace(parMS, dns.childDefineNamespace[i]);
                }
            }
        }
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            ProjectData data = ProjectManager.data;
            AddDefineNamespace( ModuleManager.instance.selfModule, data.namespaceRoot, false );

            var fileList = data.compileFileList;
            var filter = data.compileFilter;

            for( int i = 0; i < fileList.Count; i++ )
            {
                var fld = fileList[i];
                
                if(IsCanAddFile( filter, fld ) )
                {
                    ProjectCompile.AddFileParse( fld.path );
                }
            }

            if(s_CompileBeforeFunction!=null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }

        }
        public static bool IsCanAddFile(CompileFilterData cfd, CompileFileData fileData )
        {
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
