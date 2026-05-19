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
using System.IO;
using System.Text;
using SimpleLanguage.Project;

namespace SimpleLanguage.Logging
{
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
        public LogType logType { get; set; } = LogType.Info;
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

            m_SB.Append($"[{time.ToString("hh:mm:ss fff")}] ");
            m_SB.Append($"[{logType.ToString()}] ");
            m_SB.Append($"[{errorType.ToString()}] " );
            m_SB.Append(" [" + error.ToString() + "] ");

            if (!string.IsNullOrEmpty(filePath))
            {
                m_SB.Append($"Position:[{filePath}_{sourceBeginLine}_{sourceEndLine}] ");
            }

            if( !string.IsNullOrWhiteSpace(message ) )
            {
                m_SB.Append($" Info:[" + message + "]");
            }
            if (!string.IsNullOrWhiteSpace(extendMessage))
            {
                m_SB.Append($" Extend:[" + extendMessage + "]");
            }
            if (!string.IsNullOrEmpty(this.advan))
            {
                m_SB.Append($" Tips:[" + advan + "] \n");
            }
            if (!string.IsNullOrEmpty(this.demo))
            {
                m_SB.Append($" Demo:[" + demo + "] \n");
            }

            return m_SB.ToString();
        }
    }
    public class Log
    {
        /// <summary>未设置 <c>SIMPLELANG_LOGS_DIR</c> 时的 Front 日志路径（与 Core 默认导出树一致）。</summary>
        public const string FrontLogDefaultPath = @"E:\project\lang\simple_language\out\export\Core\Logs\Front.txt";

        /// <summary>Front 编译器文本日志路径；加载 jsonc 后为 <c>{export.outputDir}/{moduleName}/Logs/Front.txt</c>。</summary>
        public static string FrontLogFilePath =>
            ResolveUnderLogsDir(ProjectOutputEnvironment.FrontLogFileName);

        static string ResolveUnderLogsDir(string fileName)
        {
            var dir = Environment.GetEnvironmentVariable(ProjectOutputEnvironment.LogsDirEnv);
            if (!string.IsNullOrWhiteSpace(dir))
                return Path.Combine(Path.GetFullPath(dir.Trim()), fileName);
            return FrontLogDefaultPath;
        }

        static ConcurrentQueue<LogData> m_LogDataList = new ConcurrentQueue<LogData>();
        static readonly object m_FileLock = new object();
        static bool m_LogFilePathPrinted = false;
        static bool m_ResetFileBeforeNextWrite = true;
        static string s_LastResolvedFrontLogPath = string.Empty;

        /// <summary>在每次完整 Front 编译开始前调用，清空当前 <see cref="FrontLogFilePath"/> 并重新记录本会话。</summary>
        public static void ResetFixedLogFileForNewSession()
        {
            lock (m_FileLock)
            {
                m_ResetFileBeforeNextWrite = true;
                var p = FrontLogFilePath;
                if (!string.Equals(p, s_LastResolvedFrontLogPath, StringComparison.OrdinalIgnoreCase))
                    m_LogFilePathPrinted = false;
            }
        }

        static void WriteLineToFile(string line)
        {
            lock (m_FileLock)
            {
                var path = FrontLogFilePath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists( dir ) )
                {
                    Directory.CreateDirectory(dir);
                }

                if (m_ResetFileBeforeNextWrite)
                {
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                    m_ResetFileBeforeNextWrite = false;
                }

                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                if (!m_LogFilePathPrinted)
                {
                    Console.WriteLine($"[FrontLog] OutputPath: {path}");
                    m_LogFilePathPrinted = true;
                }
                s_LastResolvedFrontLogPath = path;
            }
        }

        public static void AddLog( LogData data )
        {
            m_LogDataList.Enqueue(data);
            var line = data.ToString();
            Console.WriteLine(line);
            WriteLineToFile(line);
        }
        //-------------------------------Project----------------------------------------------
        public static LogData AddProjectLog(LID lid, string msg, params object[] par)
        {
            return WriteCoreByToken(lid, EErrorType.Project, null, par, msg );
        }

        public static LogData AddProjectLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.Project, token, null, ""); ;
        }
        //--------------------------------Process----------------------------------------------
        public static LogData AddProcessLog(LID lid, string msg, params object[] par)
        {
            return WriteCoreByToken(lid, EErrorType.Process, null, par, msg);
        }
        public static LogData AddProcessLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.Process, token, null, ""); ;
        }
        //--------------------------------Token----------------------------------------------
        public static LogData AddTokenByString( LID lid, string path, int sLine, int sChar, int eLine, int eChar, string msg)
        {
            var token = new Token(path, ETokenType.None, sLine, sChar, eLine, eChar);
            return WriteCoreByToken(lid, EErrorType.ParseToken, token, null, msg); ;
        }
        public static LogData AddTokenLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseToken, null, null, msg); ;
        }
        public static LogData AddTokenLog(LID lid, string msg, Token token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.ParseToken, token, null, ""); 
        }

        //--------------------------------Node----------------------------------------------
        public static LogData AddNodeLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseNode, null, null, msg);
        }
        public static LogData AddNodeLog(LID lid, Token token, string msg, params object[] objs )
        {
            return WriteCoreByToken(lid, EErrorType.ParseNode, token, objs, msg);
        }
        //--------------------------------FileMeta----------------------------------------------
        public static LogData AddFileMetaLog(LID lid, Token token, string msg = "" )
        {
            return WriteCoreByToken(lid, EErrorType.ParseFile, token, new object[1] { token }, msg);
        }
        public static LogData AddFileMetaLog(LID lid, string msg)
        {
            return WriteCoreByToken(lid, EErrorType.ParseFile, null, null, msg);
        }
        public static LogData AddFileMetaLog(LID lid, string msg, params object[] objs)
        {
            Token token = null;
            if( objs.Length > 0 )
            {
                token = objs[0] as Token;
            }
            return WriteCoreByToken(lid, EErrorType.ParseFile, token, objs, msg);
        }

        //--------------------------------MetaCore----------------------------------------------
        public static LogData AddMetaCoreLog(LID lid, string msg, params object[] objs)
        {
            return WriteCoreByToken(lid, EErrorType.ParseMeta, null, objs, msg);
        }
        public static LogData AddMetaCoreLog(LID lid, Token token, string msg, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, objs, msg);
        }
        public static LogData AddMetaCoreLog(LID lid, List<Token> tokens, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            Token token = null;
            if (tokens.Count > 0)
            {
                token = tokens[0];
            }
            return WriteCoreByToken(lid, EErrorType.ParseMeta, token, tokens.ToArray(), "");
        }
        //-------------------------------GenIR----------------------------------------------        
        public static LogData AddIRLog(LID lid, string msg, params object[] objs)
        {
            return WriteCoreByToken(lid, EErrorType.GenIR, null, objs, msg);
        }
        public static LogData AddIRLog(LID lid, Token token, string msg, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            return WriteCoreByToken(lid, EErrorType.GenIR, token, objs, msg);
        }

        private static LogData WriteCoreByToken(
            LID lid,
            EErrorType errorType,
            Token token,
            object[] objects,
            string extendMessage )
        {
            if( !LogManager.TryGet( (int)lid, out var errorDefine ) )
            {
                return null;
            }

            var ld = new LogData()
            {
                error = lid,
                errorType = errorType,
                logType = errorDefine.LogType,
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
                ld.extendMessage = extendMessage;
            }
            AddLog(ld);

            //HandleBlocking(errorDefine, ld.message);

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
            WriteLineToFile("----------错误收集 开始---------------------");

            foreach ( var  ld in m_LogDataList.ToArray() )
            {
                var line = ld.ToString();
                Console.WriteLine(line);
                WriteLineToFile(line);
            }
            Console.WriteLine("----------错误收集 结束---------------------");
            WriteLineToFile("----------错误收集 结束---------------------");

            Console.WriteLine($"[FrontLog] OutputPath: {FrontLogFilePath}");
        }
    }
}