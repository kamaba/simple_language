
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

namespace SimpleLanguage.Project
{
    public class ProjectCompile
    {
        public static ProjectConfig config = null;
        public bool isThreadCompile = false;

        public static bool isLoaded = false;
        
        public static int structParseCount = 0;
        public static int buildParseCount = 0;
        public static int grammerParseCount = 0;
        public static int parseListCount = 0;
        public static string rootPath = "";
        public static List<FileParse> fileParseList = new List<FileParse>();

        public static ProjectCompileFunction compileFunction = null;
        public static void Compile( string path )
        {
            if( !isLoaded )
            {
                config = new ProjectConfig(path);
                isLoaded = true;
                config.Load();
            }
            int index = path.LastIndexOf("\\");
            if( index != -1 )
            {
                rootPath = path.Substring(0, index );
            }

            ProjectCompileFunction.ProjectCompileBefore();

            CoreMetaClassManager.instance.Init();

            structParseCount = 0;
            buildParseCount = 0;
            grammerParseCount = 0;

            parseListCount = fileParseList.Count;

            FileListStructParse();

            ProjectCompileFunction.ProjectCompileAfter();

        }

        public static void AddFileParse( string path )
        {
            var fp = new FileParse(Path.Combine(rootPath, path), new ParseFileParam() );
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
            for ( int i = 0; i < fileParseList.Count; i++ )
            {
                fileParseList[i].CheckExtendAndInterface();
            }
            ClassManager.instance.Parse();
            MetaVariableManager.instance.ParseExpress();

            ProjectCompileFunction.Parse();
            MethodManager.instance.ParseExpress();
            //ClassManager.instance.PrintAlllClassContent();
            MethodManager.instance.ParseStatements();
            Console.Write(ModuleManager.instance.ToFormatString() + Environment.NewLine);    
            IRManager.instance.TranslateIR();
        }
    }
}
