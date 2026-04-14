//****************************************************************************
//  File:      ProjectManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager project enter and compile 
//****************************************************************************

using SimpleLanguage.Core;


namespace SimpleLanguage.Project
{
    public enum EUseDefineType
    {
        NoUseProjectConfigNamespace,        //不使用项目内部配置
        LimitUseProjectConfigNamespace,//限制使用配置后的命名空间 类自由创作
        LimitUseProjectConfigNamespaceAndClass,//限制使用配置后的命名空间与类
    }
    public class ProjectManager
    {
        public static string projectPath { get; set; } = "";
        public static ProjectConfig config => m_Config;
        public static EUseDefineType useDefineNamespaceType { get; set; } = EUseDefineType.NoUseProjectConfigNamespace;

        public static bool useGenMetaClass { get; set; } = false;
        public static bool compileUseTemplateClassGenClassFunction { get; set; } = false;
        public static bool isUseNamespaceSearch { get; set; } = true;
        public static bool isUseForceSemiColonInLineEnd { get; set; } = false;
        // 第一位是否只能使用this. base.的方式
        public static bool isFirstPosMustUseThisBaseOrStaticClassName { get; set; } = false;

        // central project configuration (replaces legacy ProjectData for config-only usage)
        static ProjectConfig m_Config = new ProjectConfig();
        public static string rootPath = "";

        public static bool isSupportConstructionFunctionOnlyBraceType = true;  //是否支持构造函数使用 仅{}形式    Class1{ a = {} } 不支持
        public static bool isSupportConstructionFunctionConnectBraceType = true;  //是否支持构造函数名称后边加{}形式    Class1{ a = Class2(){} } 不支持
        public static bool isSupportConstructionFunctionOnlyParType = true; //是否支持构造函数使用 仅()形式    Class1{ a = () } 不支持
        public static bool isSupportInExpressUseStaticMetaMemeberFunction = true;   //是否在成员支持静态函数的
        public static bool isSupportInExpressUseStaticMetaVariable = true;     //是否在成员中支持静态变量
        public static bool isSupportInExpressUseCurrentClassNotStaticMemberMetaVariable = true;  //是否支持在表达式中使用本类或父类中的非静态变量
       

        public static MetaData globalData = new MetaData( "global", false, true, false );
        internal static string currentProject;

        public static void SetConfig(ProjectConfig cfg)
        {
            m_Config = cfg ?? new ProjectConfig();
        }

        public static void Run( string path, CommandInputArgs cinputArgs )
         {
            int index = path.LastIndexOf("\\");
            if (index != -1)
            {
                rootPath = path.Substring(0, index);
            }
            else
            {
                index = path.LastIndexOf("/");
                if( index != -1 )
                {
                    rootPath = path.Substring(0, index);
                }
            }                // path 现在是 .sp 配置文件路径，ProjectCompile 会基于它加载 <ProjectName>.jsonc
            ProjectCompile.Compile(path);
        }
    }
}