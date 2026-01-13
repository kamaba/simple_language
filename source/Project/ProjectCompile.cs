//****************************************************************************
//  File:      ProjectCompile.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description:  compile configuration in the .sp file. and execute configuration relative logic.
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.IR;

using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Diagnostics;
using SimpleLanguage.Compile;
using Tomlyn;
using Tomlyn.Model;
using SimpleLanguage.Logging;

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

        private static FileMeta m_ProjectFile = null;

        public static void LoadProject(string spFilePath)
        {
            if (string.IsNullOrEmpty(spFilePath))
            {
                Debug.Write("Error LoadProject 传入的 .sp 路径为空!!");
                return;
            }

            // 1. 根据 .sp 路径确定项目目录
            string projectDir = Path.GetDirectoryName(spFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            {
                Debug.Write("Error 项目加载路径不正确!!");
                return;
            }

            ProjectManager.projectPath = projectDir;

            // 2. 使用 .sp 文件名(不含扩展名)作为项目名，加载 <ProjectName>.toml
            string projectName = Path.GetFileNameWithoutExtension(spFilePath);
            string tomlFileName = projectName + ".toml";
            string tomlPath = Path.Combine(projectDir, tomlFileName);
            System.Diagnostics.Debug.WriteLine($"[LoadProject] using config: {tomlPath}");
             if (!File.Exists(tomlPath))
             {
                 Debug.Write($"Error 项目加载路径没有找到 {tomlFileName} 配置文件!!");
                 return;
             }

            string tomlText = File.ReadAllText(tomlPath);
            TomlTable model = Toml.ToModel(tomlText);

            ProjectConfig config = ProjectTomlLoader.FromModel(model);
            ProjectManager.currentProject = new Project(config);

            // 3. 后续逻辑仍然可以保留 m_ProjectFile，用于旧的基于 FileMeta 的流程
            if (m_ProjectFile == null)
            {
                string compilefile = projectName + ".sp";

                var fp = new FileParse(compilefile, new ParseFileParam());
                fp.structParseComplete = StructParseComplete;
                fp.buildParseComplete = BuildParseComplete;
                fp.grammerParseComplete = GrammerParseComplete;

                m_ProjectFile = fp.file;

                //fileParseList.Add(fp);
            }
        }

        public static void Compile( string path )
        {
            if( !isLoaded )
            {
                isLoaded = true;
                // 这里的 path 现在视为 .sp 文件路径
                LoadProject(path);
            }
            CoreMetaClassManager.instance.Init();

            ProjectClass.ProjectCompileBefore();


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
            var fp = new FileParse( path, new ParseFileParam() );
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
