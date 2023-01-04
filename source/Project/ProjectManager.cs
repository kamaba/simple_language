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
        public static bool isUseDefineNamespace { get; set; } = false;
        public static void Run( string path, CommandInputArgs cinputArgs )
        {
            ProjectCompile.Compile(path);

            if (!cinputArgs.isTest)
                ProjectCompileFunction.RunMain();
            else
                ProjectCompileFunction.RunTest();
        }
    }
}