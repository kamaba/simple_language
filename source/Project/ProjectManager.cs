//****************************************************************************
//  File:      ProjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager project enter and compile 
//****************************************************************************

using SimpleLanguage.Core;
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
        public static bool isUseDefineNamespace { get; set; } = true;
        public static bool isUseForceSemiColonInLineEnd { get; set; } = false;
        // 第一位是否只能使用this. base.的方式
        public static bool isFirstPosMustUseThisBaseOrStaticClassName { get; set; } = false;

        static ProjectData m_Data = new ProjectData( "ProjectData", false );
        public static string rootPath = "";

        public static MetaData globalData = new MetaData( "global", false );
        public static void Run( string path, CommandInputArgs cinputArgs )
        {
            int index = path.LastIndexOf("\\");
            if (index != -1)
            {
                rootPath = path.Substring(0, index);
            }

            ProjectCompile.Compile(path, m_Data);

            if (!cinputArgs.isTest)
                ProjectClass.RunMain();
            else
                ProjectClass.RunTest();
        }
    }
}