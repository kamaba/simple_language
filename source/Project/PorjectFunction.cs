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
    public class ProjectCode
    {
        private FileMeta m_File = null;
        public ProjectCode( FileMeta fm )
        {
            m_File = fm;
        }
        public void CreateNamespace()
        {
            m_File.CreateNamespace();
        }
        public void CombineFileMeta()
        {
            m_File.CombineFileMeta();
        }
        public void CheckExtendAndInterface()
        {
            m_File.CheckExtendAndInterface();
        }
        public string ToFormatString()
        {
            return m_File.ToFormatString();
        }
        public void PrintFormatString()
        {
            Console.Write(m_File.ToFormatString());
        }
    }
    public class ProjectCompileFunction
    {
        public static MetaFunction s_MainFunction = null;
        public static MetaFunction s_TestFunction = null;
        public static MetaFunction s_CompileBeforeFunction = null;
        public static MetaFunction s_CompileAfterFunction = null;
        static ProjectCode pcode = null;
        public ProjectCompileFunction( )
        {
        }
        public static void Parse()
        {
            //pcode.StructParse();
            //pcode.CombineFileMeta();
            //pcode.CheckExtendAndInterface();
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

        }
    }
}
