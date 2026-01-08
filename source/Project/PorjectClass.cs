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
using SimpleLanguage.IR;
using System.Diagnostics;

using SimpleLanguage.Compile;

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
        // legacy namespace tree building via DefineStruct is currently not driven by TOML
        // kept as a no-op placeholder to avoid breaking callers
        public static void AddDefineNamespace(MetaNode parentRoot, object _, bool isAddCurrent = true)
        {
            // namespace layout can be rebuilt later based on ProjectConfig if needed
        }
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            // 使用 TOML 基于的 ProjectConfig 填充编译文件列表
            var cfg = ProjectManager.currentProject?.Config;
            if (cfg == null)
                return;

            var fileList = cfg.CompileFiles.Files;
            var filter = cfg.CompileFilter;

            System.Diagnostics.Debug.WriteLine($"[Project] compileFiles count in config = {fileList.Count}");
 
            for (int i = 0; i < fileList.Count; i++)
            {
                var fld = fileList[i];

                if (IsCanAddFile(filter, fld))
                {
                    ProjectCompile.AddFileParse(fld.Path);
                }
            }

            if (s_CompileBeforeFunction!=null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }

        }
        public static bool IsCanAddFile(ProjectConfig.CompileFilterSection cfd, ProjectConfig.CompileFileItem fileData )
        {
            if (cfd == null) return true;

            // group 过滤
            if (!cfd.IsAllGroup)
            {
                if (cfd.Groups.Count > 0 && !cfd.Groups.Contains(fileData.Group))
                    return false;
            }

            // tag 过滤
            if (!cfd.IsAllTag)
            {
                if (cfd.Tags.Count > 0 && !cfd.Tags.Contains(fileData.Tag))
                    return false;
            }

            // ignore 标志
            if (fileData.Ignore)
                return false;

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
