//****************************************************************************
//  File:      ProjectCompile.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description:  compile configuration in the .sp file. and execute configuration relative logic.
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Diagnostics;

namespace SimpleLanguage.Project
{
    public class ProjectCompile
    {
        public bool isThreadCompile = false;
        public static bool isLoaded = false;
        public static FileMeta projectFileMeta => m_ProjectFile;
        
        public static int structParseCount = 0;
        public static int buildParseCount = 0;
        public static int grammerParseCount = 0;
        public static int parseListCount = 0;
        public static List<FileParse> fileParseList = new List<FileParse>();

        private static ProjectData m_Data = null;
        private static string m_FileContentString = null;
        private static FileMeta m_ProjectFile = null;
        private static LexerParse m_LexerParse = null;
        private static TokenParse m_TokenParse = null;
        private static StructParse m_ProjectBuild = null;
        private static ProjectParse m_ProjectParse = null;

        public static void LoadProject()
        {
            DirectoryInfo directory = new DirectoryInfo(ProjectManager.projectPath);

            if( !directory.Exists)
            {
                Debug.Write("Error 项目加载路径不正确!!");
            }

            string[] paths = Directory.GetFiles(ProjectManager.projectPath, "*.sp", SearchOption.TopDirectoryOnly);
            if( paths.Length == 0 )
            {
                Debug.Write("Error 项目加载路径没有找到sp文件!!");
            }


            if (!File.Exists(paths[0]))
            {
                Debug.Write("Error 项目加载路径不正确!!");
                return;
            }
            m_ProjectFile = new FileMeta(paths[0]);

            byte[] buffer = File.ReadAllBytes(paths[0]);
            m_FileContentString = System.Text.Encoding.UTF8.GetString(buffer);

            m_LexerParse = new LexerParse(paths[0], m_FileContentString);
            m_LexerParse.ParseToTokenList();

            m_TokenParse = new TokenParse(m_ProjectFile, m_LexerParse.GetListTokensWidthEnd());

            m_TokenParse.BuildStruct();

            m_ProjectBuild = new StructParse(m_ProjectFile, m_TokenParse.rootNode);

            m_ProjectBuild.ParseRootNodeToFileMeta();

            m_ProjectParse = new ProjectParse(m_ProjectFile, m_Data);

            m_ProjectParse.ParseProject();

            m_ProjectFile.SetDeep(0);

            ProjectClass.ParseCompileClass();

            Debug.Write(m_ProjectFile.ToFormatString());
        }

        public static void Compile( string path, ProjectData pd )
        {
            if( !isLoaded )
            {
                ProjectManager.projectPath = path;
                m_Data = pd;
                isLoaded = true;
                LoadProject();
            }

            ProjectClass.ProjectCompileBefore();

            CoreMetaClassManager.instance.Init();

            structParseCount = 0;
            buildParseCount = 0;
            grammerParseCount = 0;

            parseListCount = fileParseList.Count;

            FileListStructParse();

            ClassManager.instance.AddMetaClass( ProjectManager.globalData );

            //ProjectClass.ParseProjectClass();

            ProjectClass.ProjectCompileAfter();

            Console.WriteLine(ModuleManager.instance.selfModule.metaNode.ToFormatString());

            IRManager.instance.TranslateIR();
        }

        public static void AddFileParse( string path )
        {
            var fp = new FileParse(Path.Combine( ProjectManager.rootPath, path), new ParseFileParam() );
            fp.structParseComplete = StructParseComplete;
            fp.buildParseComplete = BuildParseComplete;
            fp.grammerParseComplete = GrammerParseComplete;
            fileParseList.Add(fp);
        }
        public static bool CheckFileList()
        {
            bool isSuccess = true;
            for (int i = 0; i < fileParseList.Count; i++)
            {
                if( !fileParseList[i].IsExists() )
                {
                    isSuccess = false;
                    Debug.Write("没有找到要编译的文件: " + fileParseList[i].filePath);
                    break;
                }
            }
            return isSuccess;
        }
        public static void FileListStructParse()
        {
            if (!CheckFileList()) return;
            for (int i = 0; i < fileParseList.Count; i++)
            {
                fileParseList[i].StructParse();

                Log.AddProcess( EProcess.StructMeta, EError.StructFileMetaEnd, fileParseList[i].ToFormatString());
            }
        }
        public static void StructParseComplete()
        {
            structParseCount++;
            if(structParseCount >= parseListCount)
            {
                CompileFileAllEnd();
            }
        }
        public static void BuildParseComplete()
        {
            buildParseCount++;
            if( buildParseCount < parseListCount )
            {
                return;
            }
        }
        public static void GrammerParseComplete()
        {
            grammerParseCount++;
            if (grammerParseCount < parseListCount)
                return;

            Debug.Write("");
        }
        public static void Update(object sender, ElapsedEventArgs e)
        {
            //timeAdd += 100;
            //Debug.Write("currentTime: " + timeAdd.ToString());
        }
        public static void CompileFileAllEnd()
        {           
            for ( int i = 0; i < fileParseList.Count; i++ )
            {
                fileParseList[i].CreateNamespace();
            }
            //NamespaceManager.instance.PrintAllNamespace();
            for (int i = 0; i < fileParseList.Count; i++)
            {
                fileParseList[i].CombineFileMeta();
            }

            for (int i = 0; i < m_ProjectFile.fileMetaClassList.Count; i++)
            {
                var fns = m_ProjectFile.fileMetaClassList[i];

                if (fns.name == "ProJectConfig"
                    || fns.name == "Compile")
                {
                    continue;
                }
                ClassManager.instance.AddClass(fns);
            }

            ClassManager.instance.ParseInitMetaClassList();
            m_ProjectParse.ParseGlobalVariable();

            ClassManager.instance.HandleExtendMember();
            ClassManager.instance.CheckInterfaces();
            ClassManager.instance.ParseDefineComplete();

            ClassManager.instance.ParseMemberEnumExpress();
            MetaVariableManager.instance.ParseMetaDataMemberExpress();
            MetaVariableManager.instance.ParseMetaClassMemberExpress();

            MethodManager.instance.ParseStatements();

            //ClassManager.instance.UpdateMetaGenTemplateClassHandle();

            ModuleManager.instance.selfModule.metaNode.SetDeep(0);


            ClassManager.instance.PrintAlllClassContent();


            Log.PrintLog();

            Debug.WriteLine("-------------------------解析完成后的格式输出 开始--------------------------");
            //Debug.WriteLine(ModuleManager.instance.ToFormatString());
            Debug.WriteLine(ModuleManager.instance.selfModule.metaNode.ToFormatString());
            Debug.WriteLine("-------------------------解析完成后的格式输出 结束--------------------------");
        }
    }
}
