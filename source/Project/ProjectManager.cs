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
    public class ProjectManager
    {
        public static bool isUseDefineNamespace { get; set; } = false;
        public static void Run( string path, bool isRun )
        {
            ProjectCompile.Compile(path);

            if (isRun)
                ProjectCompileFunction.RunMain();
            else
                ProjectCompileFunction.RunTest();
        }
    }
}