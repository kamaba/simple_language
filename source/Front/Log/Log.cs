//****************************************************************************
//  File:      Log.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SimpleLanguage.Logging
{
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
    public enum EErrorType
    {
        None,
        Project,    //初始化工程
        Process,
        ParseToken,    //识别token
        ParseNode,     //识别node
        ParseFile,         //构建FileMeta文件
        ParseMeta,             //构建元数据
        GenIR,                  //生成IR
    }
    public class LogData
    {
        public LID error { get; set; } = LID.None;
        public EErrorType errorType { get; set; } = EErrorType.None;
        public string message { get; set; }
        public string filePath { get; set; }
        public int sourceBeginLine { get; set; }         //开始所在行
        public int sourceBeginChar { get; set; }         //开始所在列
        public int sourceEndLine { get; set; }            //结束所在行
        public int sourceEndChar { get; set; }            //结束所在行
        public string demo { get; set; }
        public string advan { get; set; }
        public string extendMessage { get; set; }
        public DateTime time { get; set; }
        public LogData()
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
            if (!string.IsNullOrWhiteSpace(extendMessage))
            {
                m_SB.Append($" Extend: [" + extendMessage + " ]");
            }

            return m_SB.ToString();
        }
    }
    public class Log
    {
        static ConcurrentQueue<LogData> m_LogDataList = new ConcurrentQueue<LogData>();

        public static void AddLog( LogData data )
        {
            m_LogDataList.Enqueue(data);

            Console.WriteLine(data.ToString());
        }
        //-------------------------------Project----------------------------------------------
        public static LogData AddProjectLog(LID lid, string msg, params object[] par)
        {
            return WriteCoreByToken(lid, EErrorType.Project, null, null, msg, par );
        }

        public static LogData AddProjectLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.Project, token, null, "", null); ;
        }
        //--------------------------------Process----------------------------------------------
        public static LogData AddProcessLog(EProcess proc, LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, null, msg, null); ;
        }
        //--------------------------------Token----------------------------------------------
        public static LogData AddTokenByString( LID lid, string path, int sLine, int sChar, int eLine, int eChar, string msg)
        {
            var token = new Token(path, ETokenType.None, sLine, sChar, eLine, eChar);
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, msg, null); ;
        }
        public static LogData AddTokenLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, null, msg, null); ;
        }
        public static LogData AddTokenLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, "", null); 
        }

        //--------------------------------Node----------------------------------------------
        public static LogData AddNodeLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, null, "", null);
        }

        public static LogData AddNodeLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, "", null);
        }
        //--------------------------------FileMeta----------------------------------------------
        public static LogData AddFileMetaLog(LID lid, Token token)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, new object[1] { token }, "", null );
        }
        public static LogData AddFileMetaLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, null, msg, null);
        }
        public static LogData AddFileMetaLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, "", null);
        }

        //--------------------------------MetaCore----------------------------------------------
        public static LogData AddMetaCoreLog(LID lid, string msg)
        {
            return null;//WriteCore(lid, EErrorType.ParseMeta, null, null, msg, null);
        }
        public static LogData AddMetaCoreLog(LID lid, List<Token> tokens, params object[] objs )
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            Token token = null;
            if( tokens.Count > 0 )
            {
                token = tokens[0];
            }
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, tokens.ToArray(), "", null);
        }
        public static LogData AddMetaCoreLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, "", null );
        }

        //-------------------------------GenIR----------------------------------------------
        public static LogData AddIRLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, null, "", null); ;
        }

        public static LogData AddIRLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, null, "", null); ;
        }

        private static LogData WriteCoreByToken(
            LID lid,
            EErrorType errorType,
            Token token,
            object[] objects,
            string extendMessage,
            object[] extendsObject )
        {
            if( !LogManager.TryGet( (int)lid, out var errorDefine ) )
            {
                return null;
            }

            var ld = new LogData()
            {
                error = lid,
                errorType = errorType,
                time = DateTime.Now
            };
            if (token != null)
            {
                ld.filePath = token.path;
                ld.sourceBeginLine = token.sourceBeginLine;
                ld.sourceBeginChar = token.sourceBeginChar;
                ld.sourceEndLine = token.sourceEndLine;
                ld.sourceEndChar = token.sourceEndChar;
            }

            ld.time = DateTime.Now;
            ld.advan = errorDefine.FixedTipArray[LogManager.LanguageIndex];
            ld.demo = errorDefine.Demo;
            if( errorDefine.ParamCount  > 0 )
            {
                if(objects != null )
                {
                    if(errorDefine.ParamCount != objects.Length )
                    {
                        extendMessage = extendMessage + "传入参数与要求参数不对应!";
                    }
                    else
                    {
                        string message = errorDefine.MessageTemplateArray[LogManager.LanguageIndex];
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ld.message = string.Format(message, objects);
                        }
                    }
                }
                else
                {
                    extendMessage = extendMessage + "传入参数与要求参数不对应!";
                }
            }
            else
            {
                ld.message = errorDefine.MessageTemplateArray[LogManager.LanguageIndex];
            }

            if (!string.IsNullOrWhiteSpace(extendMessage))
            {
                if(extendsObject != null )
                {
                    ld.extendMessage = string.Format(extendMessage, extendsObject);
                }
                else
                {
                    ld.extendMessage = extendMessage;
                }
            }
            AddLog(ld);

            HandleBlocking(errorDefine, ld.message);

            if ( LogManager.Options.EnableAssertFeature && errorDefine.EnableAssert)
            {
                Debug.Assert(false, $"Error ID: {errorDefine.Id}, Message: {ld.message}");
            }
            
            return ld;
        }
        private static void HandleBlocking(ErrorDefinition def, string message)
        {
            bool isAssert = def.LogType == LogType.Assert;
            bool isError = def.LogType == LogType.Error;

            if (isAssert && (!LogManager.Options.EnableAssertFeature || !def.EnableAssert))
            {
                return;
            }

            bool shouldBlockCurrent = def.BlockOnErrorAssert
                || (isAssert && LogManager.Options.BlockOnAssert)
                || (isError && LogManager.Options.BlockOnError);

            bool shouldAbortCompilation = def.AbortCompilation
                || (isAssert && LogManager.Options.AbortCompilationOnAssert)
                || (isError && LogManager.Options.AbortCompilationOnError);

            if (shouldBlockCurrent || shouldAbortCompilation)
            {
                throw new CompilationAbortException(def.Id, message, shouldAbortCompilation);
            }
        }
        public static void PrintLog()
        {
            Console.WriteLine("----------错误收集 开始---------------------");

            foreach ( var  ld in m_LogDataList.ToArray() )
            {
                Console.WriteLine(ld.ToString());
            }
            Console.WriteLine("----------错误收集 结束---------------------");
        }
    }
}