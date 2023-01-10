//****************************************************************************
//  File:      ProjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager project enter and compile 
//****************************************************************************

using SimpleLanguage.Project;
using System.IO;

namespace SimpleLanguage.Parse
{
    public class CommandInputArgs
    {
        public bool isTest { get; set; } = false;
        public bool isPrintToken { get; set; } = false;

        public CommandInputArgs( string[] args )
        {

        }
    }
    public class ProjectManager
    {
        public static ProjectData data => m_Data;
        public static bool isUseDefineNamespace { get; set; } = false;

        static ProjectData m_Data = new ProjectData();
        public static string rootPath = "";
        public static void Run( string path, CommandInputArgs cinputArgs )
        {
            int index = path.LastIndexOf("\\");
            if (index != -1)
            {
                rootPath = path.Substring(0, index);
            }

            ProjectCompile.Compile(path, m_Data);

            if (!cinputArgs.isTest)
                ProjectCompileFunction.RunMain();
            else
                ProjectCompileFunction.RunTest();
        }
    }
}