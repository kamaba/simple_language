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
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
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
                Log.AddProjectLog(LID.ShowExtendMessage, "", spFilePath );
                return;
            }

            // 1. 根据 .sp 路径确定项目目录
            string projectDir = Path.GetDirectoryName(spFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            {
                Log.AddProjectLog(LID.ProjectRootPathNotFound, "项目加载路径不正确!!", spFilePath);
                return;
            }

            ProjectManager.projectPath = projectDir;

            // 2. 使用 .sp 文件名(不含扩展名)作为项目名，加载 <ProjectName>.jsonc
            string projectName = Path.GetFileNameWithoutExtension(spFilePath);
            string jsoncFileName = projectName + ".jsonc";
            string jsoncPath = Path.Combine(projectDir, jsoncFileName);
            if (!File.Exists(jsoncPath))
            {
                Log.AddProjectLog(LID.ProjectShowConfigSuccessPath,"", jsoncPath);
                return;
            }

            string jsoncText = File.ReadAllText(jsoncPath);
            ProjectConfig config = null;
            try
            {
                config = ProjectJsoncLoader.FromJsonc(jsoncText);
            }
            catch( Exception e )
            {
                Log.AddProjectLog(LID.ProjectParseConfigFailed, "", jsoncPath);
                return;
            }
            // Generate project guide markdown beside .sp/.jsonc
            ProjectClass.ExportProjectGuideMarkdown(spFilePath, jsoncPath);
            ProjectManager.SetConfig(config);
            ModuleManager.instance.InitSelfModuleManager(config.Project.Name);

            // Logs / DebugCode / *.module.json 均在 {export.outputDir}/{moduleName}/（见 ProjectOutputEnvironment）。
            ProjectOutputEnvironment.ApplyFromConfig(config, projectDir, projectName);
            Log.AddProjectLog(LID.ProjectShowConfigPath, "", jsoncPath);

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
            // Disable assert crashes so compilation continues past non-fatal errors.
            LogManager.Options.EnableAssertFeature = false;

            if( !isLoaded )
            {
                isLoaded = true;
                // 这里的 path 现在视为 .sp 文件路径
                LoadProject(path);
            }
            CSharpManager.InitCanSearchAssemblyList();

            CoreMetaClassManager.instance.Init();
            SystemMethodCallDeclarationRegistry.LoadConfigSystemCall();

            // Load reference modules AFTER Core inner types are built.
            // This allows a compiled Core reference to replace the C# inner-form
            // Core types with the code-based definitions.
            ProjectReferenceModuleLoader.LoadReferences(ProjectManager.config, ProjectManager.projectPath);

            ProjectClass.ProjectCompileBefore();

            structParseCount = 0;
            buildParseCount = 0;
            grammerParseCount = 0;

            parseListCount = fileParseList.Count;

            FileListStructParse();

            ClassManager.instance.AddMetaData( ProjectManager.globalData );

            //ProjectClass.ParseProjectClass();

            ProjectClass.ProjectCompileAfter();

            IRManager.instance.TranslateIR();
        }

        public static void AddFileParse( string path )
        {
            var find = fileParseList.Find(a => a.filePath == path);
            if ( find != null )
            {
                Log.AddProjectLog(LID.ShowExtendMessage, "已经添加过一次该文件: " + find.filePath);
                return;
            }

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
                    Log.AddProjectLog(LID.ShowExtendMessage, "没有找到要编译的文件: " + fileParseList[i].filePath);
                    break;
                }
            }
            return isSuccess;
        }
        public static void FileListStructParse()
        {
            Log.ResetFixedLogFileForNewSession();
            if (!CheckFileList()) return;
            // Pre-FileMeta stage: process each source file in parallel.
            //Parallel.ForEach(fileParseList, fp =>
            //{
            //    fp.StructParse();
            //});
            foreach( var v in fileParseList )
            {
                v.StructParse();
            }

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
            Log.AddProcessLog(LID.ProcessCompileMetaStart, "");
            for ( int i = 0; i < fileParseList.Count; i++ )
            {
                fileParseList[i].CreateNamespace();
            }

            for (int i = 0; i < fileParseList.Count; i++)
            {
                fileParseList[i].CombineFileMeta();
            }

            // 类结构 + 继承/接口与 extend 序就绪后注册 typealias，再收集成员定义类型（见 ClassManager 分步注释）
            TypeManager.instance.ClearProjectTypeAliases();
            ClassManager.instance.ParseInitMetaClassListThroughInheritance();
            TypeManager.instance.ResolveAllDeclaredTypeAliases(fileParseList);
            ClassManager.instance.ParseInitMetaListCollectMemberDefineMetaTypes();

            ClassManager.instance.CheckInterfaces();

            // Parse and process attributes after inheritance/interface resolution.
            // Compile-time attributes (e.g. Nickname) are applied here so that
            // subsequent member parsing and IR generation can use alias lookups.
            ClassManager.instance.ParseAttributes();

            MetaVariableManager.instance.ParseMetaMemberExpress();
            MethodManager.instance.ParseMetaMethodExpress();

            ClassManager.instance.ParseDefineComplete();

            // Inject jsonc data (root "data" + legacy global.data) into Project meta members before statements parse.
            ProjectClass.InjectProjectGlobalDataFromConfig();

            // Build per-file local{} classes after member express parsed but before statements parsing.
            LocalManager.instance.BuildFileLocalClasses(fileParseList);

            MethodManager.instance.ParseStatements();

            // After all methods parsed, inject local{} initialization calls in compile-file order.
            GlobalManager.instance.InjectGlobalInitCall();
            LocalManager.instance.InjectLocalInitCalls(fileParseList);

            // Export per-file MetaCore debug data after logic parsing is complete.
#if DEBUG
            //ClassManager.instance.UpdateMetaGenTemplateClassHandle();
            ModuleManager.instance.selfModule.metaNode.SetDeep(0);
            ModuleManager.instance.coreModule.metaNode.SetDeep(-1);
            for (int i = 0; i < fileParseList.Count; i++)
            {
                fileParseList[i].ExportMetaDebugData();
            }
#endif

            Log.AddProcessLog(LID.ProcessCompileMetaEnd, "");
        }
    }
}
