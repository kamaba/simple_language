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
        LexerParse lexerParse;
        TokenParse tokenParse;
        StructParse structBuild;
        private FileMeta m_File = null;

        public string filePath;
        public string content;
        public ProjectCode( string fp, string _content)
        {
            filePath = fp;
            content = _content;
            m_File = new FileMeta(filePath);
        }
        public void StructParse()
        {
            lexerParse = new LexerParse(filePath, content);
            content = string.Empty;
            
            lexerParse.ParseToTokenList();
            
            tokenParse = new TokenParse(m_File, lexerParse.GetListTokensWidthEnd() );
            tokenParse.BuildStruct();
            
            structBuild = new StructParse(m_File, tokenParse.rootNode);
            structBuild.ParseRootNodeToFileMeta();            
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
        public ProjectCompileFunction(string p, string _content)
        {
            pcode = new ProjectCode(p, _content);
        }
        public static void Parse()
        {
            pcode.StructParse();
            pcode.CombineFileMeta();
            pcode.CheckExtendAndInterface();
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
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            var config = ProjectCompile.config.data;
            List<string> globalList = config.globalNamespaceList;
            for (int i = 0; i < globalList.Count; i++)
            {
                NamespaceManager.instance.AddNamespaceString(globalList[i]);
            }

            var fileList = config.compileFileList;
            var filter = config.filter;

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
