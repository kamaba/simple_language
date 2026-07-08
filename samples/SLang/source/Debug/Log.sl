//****************************************************************************
//  File:      Log.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Parse
{
    public enum EError
    {
        None,

        ParseTokenStart,
        UnMatchChar,
        ParseTokenEnd,

        StructMetaStart,
        MemberNeedExpress,
        AddClassNameSame,
        StructMetaEnd,


        StructFileMetaStart,
        StructClassNameRepeat,
        StructFileMetaEnd,

        ProcessStart,
        ParseFileError,
        ProcessEnd,
    }

    public enum EProcess
    {
        None,
        ParseToken,
        ParseNode,
        StructFileMeta,
        StructMeta,
        HandleClass,
        HandleMember,
        HandleSyntax,
    }

    public enum EMetaType
    {
        None,
        MetaNamespace,
        MetaClass,
        MetaExtendsClass,
        MetaData,
        MetaEnum,
        MetaMemberVariable,
        MetaMemberVariableExpress,
        MetaMemberData,
        MetaMemberDataExpress,
        MetaMemberEnumValue,
        MetaMemberEnumValueExpress,
    }
    public class LogData
    {

        public enum EErrorType
        {
            None,
            InitProject,    //初始化工程
            HandleToken,    //识别token
            HandleNode,     //识别node
            StructFileMeta,         //构建FileMeta文件
            StructMeta,             //构建元数据
            GenIR,                  //生成IR
            VM,                     //使用VM
            Process,
        }

        public EError error { get; set; } = 0;
        public EErrorType errorType { get; set; } = EErrorType.None;
        public string message { get; set; }
        public string filePath { get; set; }
        public int sourceBeginLine { get; set; }         //开始所在行
        public int sourceBeginChar { get; set; }         //开始所在列
        public int sourceEndLine { get; set; }            //结束所在行
        public int sourceEndChar { get; set; }            //结束所在行
        public string demo { get; set; }
        public string advan { get; set; }
        public DateTime time { get; set; }

        public Dictionary<EMetaType, MetaBase> valDict = new Dictionary<EMetaType, MetaBase>();

        public LogData()
        {

        }
        LogData( string msg, string path, int sline, int schar, int eline, int echar )
        {

        }
        static StringBuilder m_SB = new StringBuilder();
        public override string ToString()
        {
            m_SB.Clear();

            m_SB.Append($"类型: [{errorType.ToString()}] " );
            m_SB.Append(" Error: [" + error.ToString() + " ] ");

            if (!string.IsNullOrEmpty(filePath))
            {
                m_SB.Append($" FilePath: [{filePath}] ");
                m_SB.Append($" SLine: [{sourceBeginLine}] ");
                m_SB.Append($" ELine: [{sourceEndLine}] ");
            }

            m_SB.Append($" Info: [" + message + " ]");

            return m_SB.ToString();
        }
    }
    public class CodeFileLogData : LogData
    {
    }
    public class LexelLogData : LogData
    {

    }
    public class Log
    {
        static List<LogData> logDataList = new List<LogData>();
        public static void AddCodeFileLog( LogData data )
        {
            Console.WriteLine(data.ToString());
            logDataList.Add(data);
        }
        public static void AddInInitProject(Token token, EError err, string msg)
        {
            LogData ld = new LogData()
            {
                filePath = token.path,
                sourceBeginLine = token.sourceBeginLine,
                sourceEndLine = token.sourceEndLine,
                errorType = LogData.EErrorType.InitProject
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
        }
        public static LogData AddProcess(EProcess proc, EError err, string msg)
        {
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.Process
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);

            return ld;
        }
        public static LogData AddInHandleToken(string path, int sbl, int sel, EError err, string msg)
        {
            LogData ld = new LogData()
            {
                filePath = path,
                sourceBeginLine = sbl,
                sourceBeginChar = sel,
                errorType = LogData.EErrorType.HandleToken,
                time = DateTime.Now
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddInHandleNode(Token token, EError err, string msg)
        {
            LogData ld = new LogData()
            {
                filePath = token.path,
                sourceBeginLine = token.sourceBeginLine,
                sourceEndLine = token.sourceEndLine,
                errorType = LogData.EErrorType.HandleNode,
                time = DateTime.Now
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddInStructFileMeta(EError err, string msg)
        {
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.StructFileMeta,
                time = DateTime.Now
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddInStructFileMeta(EError err, string msg, Token token )
        {
            LogData ld = new LogData()
            {
                filePath = token.path,
                errorType = LogData.EErrorType.StructFileMeta,
                time = DateTime.Now,
                sourceBeginLine = token.sourceBeginLine,
                sourceEndLine = token.sourceEndLine,
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddInStructMeta(EError err, string msg)
        {
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.StructMeta,
                time = DateTime.Now
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddInStructMeta(EError err, string msg, Token token )
        {
            if( token == null )
            {
                token = new Token();
            }
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.StructMeta,
                time = DateTime.Now,
                sourceBeginLine = token.sourceBeginLine,
                sourceBeginChar = token.sourceBeginChar,
                sourceEndLine = token.sourceEndLine,   
                sourceEndChar = token.sourceEndChar,
                filePath = token.path,
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddGenIR(EError err, string msg)
        {
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.GenIR,
                time = DateTime.Now,
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        public static LogData AddVM( EError err, string msg )
        {
            LogData ld = new LogData()
            {
                errorType = LogData.EErrorType.VM,
                time = DateTime.Now,
            };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
            return ld;
        }
        static void AddLog( Token token, EError err, string msg )
        {
            LogData ld = new LogData() { filePath = token.path,
                sourceBeginLine = token.sourceBeginLine,
                sourceEndLine = token.sourceEndLine,  };
            ld.message = msg;
            ld.error = err;
            AddCodeFileLog(ld);
        }

        public static void PrintLog()
        {
            Console.WriteLine("----------错误收集 开始---------------------");

            foreach ( var  ld in logDataList )
            {
                Console.WriteLine(ld.ToString());
            }
            Console.WriteLine("----------错误收集 结束---------------------");
        }
    }
}