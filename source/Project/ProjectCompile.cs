//****************************************************************************
//  File:      ProjectCompile.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description:  compile configuration in the .sp file. and execute configuration relative logic.
//****************************************************************************

using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Project;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Compile.CoreFileMeta;

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
        private static string m_ProjectPath;
        private static string m_FileContentString = null;
        private static FileMeta m_ProjectFile = null;
        private static LexerParse m_LexerParse = null;
        private static TokenParse m_TokenParse = null;
        private static StructParse m_ProjectBuild = null;
        private static ProjectParse m_ProjectParse = null;

        public static void LoadProject()
        {
            if (!File.Exists(m_ProjectPath))
            {
                Console.WriteLine("Error 项目加载路径不正确!!");
                return;
            }
            m_ProjectFile = new FileMeta(m_ProjectPath);

            byte[] buffer = File.ReadAllBytes(m_ProjectPath);
            m_FileContentString = System.Text.Encoding.UTF8.GetString(buffer);

            m_LexerParse = new LexerParse(m_ProjectPath, m_FileContentString);
            m_LexerParse.ParseToTokenList();

            m_TokenParse = new TokenParse(m_ProjectFile, m_LexerParse.GetListTokensWidthEnd());

            m_TokenParse.BuildStruct();

            m_ProjectBuild = new StructParse(m_ProjectFile, m_TokenParse.rootNode);

            m_ProjectBuild.ParseRootNodeToFileMeta();

            m_ProjectParse = new ProjectParse(m_ProjectFile, m_Data);

            m_ProjectParse.ParseProject();

            m_ProjectFile.SetDeep(0);

            ProjectClass.ParseCompileClass();

            ProjectClass.ParseProjectClass();

            Console.WriteLine(m_ProjectFile.ToFormatString());
        }

        public static void Compile( string path, ProjectData pd )
        {
            if( !isLoaded )
            {
                m_ProjectPath = path;
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

            ProjectClass.ProjectCompileAfter();

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
                    Console.WriteLine("没有找到要编译的文件: " + fileParseList[i].filePath);
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

                Console.WriteLine(fileParseList[i].ToFormatString());
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

            Console.WriteLine("");
        }
        public static void Update(object sender, ElapsedEventArgs e)
        {
            //timeAdd += 100;
            //Console.WriteLine("currentTime: " + timeAdd.ToString());
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
            for ( int i = 0; i < fileParseList.Count; i++ )
            {
                fileParseList[i].CheckExtendAndInterface();
            }
            m_ProjectFile.CheckExtendAndInterface();

            ClassManager.instance.ParseFileMetaClass();

            ClassManager.instance.Parse();

            m_ProjectParse.ParseInitClassAfter();

            ModuleManager.instance.selfModule.SetDeep(0);

            MetaVariableManager.instance.ParseExpress();

            MethodManager.instance.ParseExpress();
            //ClassManager.instance.PrintAlllClassContent();
            MethodManager.instance.ParseStatements();
            Console.Write(ModuleManager.instance.ToFormatString() + Environment.NewLine);   
        }
    }
}
