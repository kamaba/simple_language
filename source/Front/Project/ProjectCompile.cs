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
using SimpleLanguage.ExportLanguage;
using CompileProcess = SimpleLanguage.Compile.Process;
using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleLanguage.Project
{
    public class ProjectCompile
    {
        public bool isThreadCompile = false;
        public static bool isLoaded = false;
        public static FileMeta projectFileMeta => m_ProjectFile;

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

            // 注册五个大阶段的所有小步骤
            InitCompileProcess();

            // 依次执行：RefModule -> File -> MetaCore -> IR
            // 导出阶段(Export)由调用方按需触发 RunPhase(Export)
            CompileProcess.ProcessManager.instance.RunToPhase(CompileProcess.ECompilePhase.IR);

            CompileProcess.ProcessManager.instance.PrintSummary();
        }

        /// <summary>向过程管理器注册五个大阶段（RefModule/File/MetaCore/IR/Export）的所有小步骤</summary>
        private static void InitCompileProcess()
        {
            var pm = CompileProcess.ProcessManager.instance;
            pm.Reset();

            // ============ 阶段1 RefModule：读取外部引入的 Module（错误只提示，不影响后续编译） ============
            pm.AddStep(CompileProcess.ECompilePhase.RefModule, "LoadRefModules", () =>
            {
                // Load reference modules AFTER Core inner types are built.
                // This allows a compiled Core reference to replace the C# inner-form
                // Core types with the code-based definitions.
                ProjectReferenceModuleLoader.LoadReferences(ProjectManager.config, ProjectManager.projectPath);
                return true;
            });

            // ============ 阶段2 File：文件编译（Token -> Node -> File），单文件错误只影响当前文件 ============
            pm.AddStep(CompileProcess.ECompilePhase.File, "PrepareFiles", () =>
            {
                Log.ResetFixedLogFileForNewSession();
                ProjectClass.ProjectCompileBefore();
                return CheckFileList();
            });
            pm.AddStep(CompileProcess.ECompilePhase.File, "Token", () => RunFileStep(fp => fp.ParseTokenStep()));
            pm.AddStep(CompileProcess.ECompilePhase.File, "Node", () => RunFileStep(fp => fp.ParseNodeStep()));
            pm.AddStep(CompileProcess.ECompilePhase.File, "File", () => RunFileStep(fp => fp.ParseFileStep()));

            // ============ 阶段3 MetaCore：全工程(含 RefModule)逻辑整合与编译 ============
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "CreateNamespace", () =>
            {
                for (int i = 0; i < fileParseList.Count; i++)
                {
                    fileParseList[i].CreateNamespace();
                }
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "CombineFileMeta", () =>
            {
                for (int i = 0; i < fileParseList.Count; i++)
                {
                    fileParseList[i].CombineFileMeta();
                }
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ParseMetaClassLink", () =>
            {
                // 类结构 + 继承/接口与 extend 序就绪后注册 typealias，再收集成员定义类型（见 ClassManager 分步注释）
                TypeManager.instance.ClearProjectTypeAliases();
                ClassManager.instance.ParseInitMetaClassListThroughInheritance();
                TypeManager.instance.ResolveAllDeclaredTypeAliases(fileParseList);
                ClassManager.instance.ParseInitMetaListCollectMemberDefineMetaTypes();

                ClassManager.instance.CheckInterfaces();
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ParseAttributes", () =>
            {
                // Parse and process attributes after inheritance/interface resolution.
                // Compile-time attributes (e.g. Nickname) are applied here so that
                // subsequent member parsing and IR generation can use alias lookups.
                ClassManager.instance.ParseAttributes();
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ParseMemberExpress", () =>
            {
                MetaVariableManager.instance.ParseMetaMemberExpress();
                MethodManager.instance.ParseMetaMethodExpress();

                ClassManager.instance.ParseDefineComplete();
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "InjectProjectData", () =>
            {
                // Inject jsonc data (root "data" + legacy global.data) into Project meta members before statements parse.
                ProjectClass.InjectProjectGlobalDataFromConfig();
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "BuildLocalClass", () =>
            {
                // Build per-file local{} classes after member express parsed but before statements parsing.
                LocalManager.instance.BuildFileLocalClasses(fileParseList);
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ParseStatements", () =>
            {
                MethodManager.instance.ParseStatements();
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "InjectInitCall", () =>
            {
                // After all methods parsed, inject local{} initialization calls in compile-file order.
                GlobalManager.instance.InjectGlobalInitCall();
                LocalManager.instance.InjectLocalInitCalls(fileParseList);
                return true;
            });
#if DEBUG
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ExportMetaDebug", () =>
            {
                // Export per-file MetaCore debug data after logic parsing is complete.
                ModuleManager.instance.selfModule.metaNode.SetDeep(0);
                ModuleManager.instance.coreModule.metaNode.SetDeep(-1);
                for (int i = 0; i < fileParseList.Count; i++)
                {
                    fileParseList[i].ExportMetaDebugData();
                }
                return true;
            });
#endif
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "AddGlobalData", () =>
            {
                ClassManager.instance.AddMetaData( ProjectManager.globalData );
                return true;
            });
            pm.AddStep(CompileProcess.ECompilePhase.MetaCore, "ProjectCompileAfter", () =>
            {
                ProjectClass.ProjectCompileAfter();
                return true;
            });

            // ============ 阶段4 IR：编译成 IR 逻辑，供导出使用 ============
            pm.AddStep(CompileProcess.ECompilePhase.IR, "TranslateIR", () =>
            {
                IRManager.instance.TranslateIR();
                return true;
            });

            // ============ 阶段5 Export：对 IR 逻辑进行 Module 导出 ============
            pm.AddStep(CompileProcess.ECompilePhase.Export, "ExportModule", () =>
            {
                ExportLangManager.Export(ExportKind.SLIR);
                return true;
            });
        }

        /// <summary>
        /// 对文件列表执行文件阶段的某个小步骤：
        /// 单个文件失败(异常/错误)只影响该文件(后续小步骤会跳过它)，不影响其它文件。
        /// 返回是否全部文件成功。
        /// </summary>
        private static bool RunFileStep( Func<FileParse, bool> stepFunc )
        {
            bool allSuccess = true;
            foreach ( var fp in fileParseList )
            {
                if ( !stepFunc( fp ) )
                {
                    allSuccess = false;
                }
            }
            return allSuccess;
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
    }
}
