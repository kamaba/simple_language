//****************************************************************************
//  File:      ProjectCompile.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description:  compile configuration in the .sp file. and execute configuration relative logic.
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using SimpleLanguage.CSharp;
using SimpleLanguage.IR;
using SimpleLanguage.Lib;
using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using SimpleLanguage.Export;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

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
                Log.AddProjectLog(LID.FilemetaAddMetaclassMetanodeDuplicateNode_10156, "", spFilePath );
                return;
            }

            // 1. 根据 .sp 路径确定项目目录
            string projectDir = Path.GetDirectoryName(spFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            {
                Log.AddProjectLog(LID.FilemetaAddMetaclassMetanodeDuplicateNode_10156, "项目加载路径不正确!!", spFilePath);
                return;
            }

            ProjectManager.projectPath = projectDir;

            // 2. 使用 .sp 文件名(不含扩展名)作为项目名，加载 <ProjectName>.jsonc
            string projectName = Path.GetFileNameWithoutExtension(spFilePath);
            string jsoncFileName = projectName + ".jsonc";
            string jsoncPath = Path.Combine(projectDir, jsoncFileName);
            System.Diagnostics.Debug.WriteLine($"[LoadProject] using config: {jsoncPath}");
            if (!File.Exists(jsoncPath))
            {
                Debug.Write($"Error 项目加载路径没有找到 {jsoncFileName} 配置文件!!");
                return;
            }

            string jsoncText = File.ReadAllText(jsoncPath);
            ProjectConfig config = ProjectJsoncLoader.FromJsonc(jsoncText);
            ProjectManager.currentProject = new Project(config);

            // 3. 后续逻辑仍然可以保留 m_ProjectFile，用于旧的基于 FileMeta 的流程
            if (m_ProjectFile == null)
            {
                string compilefile = projectName + ".sp";

                var fp = new FileParse(compilefile, new ParseFileParam());
                fp.structParseComplete = null;
                fp.buildParseComplete = null;
                fp.grammerParseComplete = null;

                m_ProjectFile = fp.file;

                fileParseList.Add(fp);
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
            CSharpManager.InitCanSearchAssemblyList();

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

            IRManager.instance.TranslateIR();
        }

        public static void AddFileParse( string path )
        {
            var fp = new FileParse( path, new ParseFileParam() );
            fp.structParseComplete = null;
            fp.buildParseComplete = null;
            fp.grammerParseComplete = null;
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
                    Debug.Assert( false, "没有找到要编译的文件: " + fileParseList[i].filePath);
                    break;
                }
            }
            return isSuccess;
        }
        public static void FileListStructParse()
        {
            if (!CheckFileList()) return;
            // Pre-FileMeta stage: process each source file in parallel.
            Parallel.ForEach(fileParseList, fp =>
            {
                fp.StructParse();
            });

            // After all FileMeta-pre stages are complete, continue with unified main-thread MetaCore pipeline.
            CompileFileAllEnd();
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

            //for (int i = 0; i < m_ProjectFile.fileMetaClassList.Count; i++)
            //{
            //    var fns = m_ProjectFile.fileMetaClassList[i];

            //    if (fns.name == "ProJectConfig"
            //        || fns.name == "Compile")
            //    {
            //        continue;
            //    }
            //    ClassManager.instance.AddClass(fns);
            //}

            ClassManager.instance.ParseInitMetaClassList();            

            ClassManager.instance.CheckInterfaces();
            ClassManager.instance.ParseDefineComplete();

            ClassManager.instance.ParseMemberEnumExpress();
            MetaVariableManager.instance.ParseMetaDataMemberExpress();
            MetaVariableManager.instance.ParseMetaClassMemberExpress();

            // Build per-file local{} classes after member express parsed but before statements parsing.
            GlobalManager.instance.BuildGlobalClass(fileParseList);
            LocalManager.instance.BuildFileLocalClasses(fileParseList);

            MethodManager.instance.ParseStatements();

            // After all methods parsed, inject local{} initialization calls in compile-file order.
            GlobalManager.instance.InjectGlobalInitCall();
            LocalManager.instance.InjectLocalInitCalls(fileParseList);

            // Export per-file MetaCore debug data after logic parsing is complete.
            for (int i = 0; i < fileParseList.Count; i++)
            {
                fileParseList[i].ExportMetaDebugData();
            }

            //ClassManager.instance.UpdateMetaGenTemplateClassHandle();

            ModuleManager.instance.selfModule.metaNode.SetDeep(0);


            // Front layer print output is disabled.
            // Debug/export content is written to DebugCode/*.txt by dedicated exporters.

            // (reserved) Export steps are invoked explicitly by Export pipeline.
        }
    }
}
