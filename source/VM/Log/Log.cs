//****************************************************************************
//  File:      Log.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimpleLanguage.Logging
{
    public enum EErrorType
    {
        None,
        Project,    //初始化工程
        Process,
        ParseIR,
        Runtime,
        Other
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

        public bool enableAssert { get; set; } = false;
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
            return m_SB.ToString();
        }
    }
    public class Log
    {
        const string LogsDirEnv = "SIMPLELANG_LOGS_DIR";

        /// <summary>未设置 <see cref="LogsDirEnv"/> 时的 VM 日志路径（与 Core 默认导出树一致）。</summary>
        public const string VmLogDefaultPath = @"E:\project\lang\simple_language\out\export\Core\Logs\VM.txt";

        /// <summary>VM 文本日志路径；加载 jsonc 后为 <c>{export.outputDir}/{moduleName}/Logs/VM.txt</c>。</summary>
        public static string VmLogFilePath => ResolveUnderLogsDir("VM.txt");

        /// <summary>未设置 <see cref="LogsDirEnv"/> 时的 print 镜像路径。</summary>
        public const string VmRunResultDefaultPath = @"E:\project\lang\simple_language\out\export\Core\Logs\Result.txt";

        /// <summary>VM <c>print</c>/<c>println</c> 镜像路径（与 <see cref="VmLogFilePath"/> 同 <c>Logs</c> 目录）。</summary>
        public static string VmRunResultFilePath => ResolveUnderLogsDir("Result.txt");

        static string ResolveUnderLogsDir(string fileName)
        {
            var dir = Environment.GetEnvironmentVariable(LogsDirEnv);
            if (!string.IsNullOrWhiteSpace(dir))
                return Path.Combine(Path.GetFullPath(dir.Trim()), fileName);
            return string.Equals(fileName, "VM.txt", StringComparison.OrdinalIgnoreCase) ? VmLogDefaultPath : VmRunResultDefaultPath;
        }

        static ConcurrentQueue<LogData> m_LogDataList = new ConcurrentQueue<LogData>();
        static readonly object s_FileLock = new object();
        static bool s_LogPathPrinted = false;
        static bool s_ResetFileBeforeNextWrite = true;

        /// <summary>在每次 VM 进程会话开始时调用，清空 <see cref="VmLogFilePath"/>。</summary>
        public static void ResetFixedLogFileForNewSession()
        {
            lock (s_FileLock)
            {
                s_ResetFileBeforeNextWrite = true;
                s_LogPathPrinted = false;
            }
        }

        static void WriteLineToFile(string line)
        {
            lock (s_FileLock)
            {
                var path = VmLogFilePath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                if (s_ResetFileBeforeNextWrite)
                {
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                    s_ResetFileBeforeNextWrite = false;
                }

                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                if (!s_LogPathPrinted)
                {
                    Console.WriteLine($"[VMLog] OutputPath: {path}");
                    s_LogPathPrinted = true;
                }
            }
        }

        public static void AddLog( LogData data )
        {
            m_LogDataList.Enqueue(data);
            var line = data.ToString();
            WriteLineToFile(line);

            switch( data.logType )
            {
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    Debug.WriteLine(line);
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Debug.WriteLine(line);
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Debug.WriteLine(line);
                    break;
                case LogType.Assert:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    if( LogManager.Options.EnableAssertFeature && data.enableAssert )
                    {
                        Debug.Assert(false, line );
                    }
                    else
                    {
                        Debug.Assert(true, line);
                    }
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(line);
                    break;
            }
        }

        /// <summary>兼容旧 VM 代码路径；消息走 Other 通道并写入 <see cref="VmLogFilePath"/>。</summary>
        public static LogData AddVM(LID lid, string msg) => AddOtherLog(lid, msg);
        //-------------------------------Project----------------------------------------------
        public static LogData AddProjectLog(LID lid, string msg, params object[] par)
        {
            return WriteCoreByToken(lid, EErrorType.Project, null, par, msg );
        }
        public static LogData AddProjectLog(LID lid, string msg, DebugInfo token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.Project, token, null, ""); ;
        }
        //--------------------------------Process----------------------------------------------
        public static LogData AddProcessLog(LID lid, string msg, params object[] par)
        {
            return WriteCoreByToken(lid, EErrorType.Process, null, par, msg);
        }
        public static LogData AddProcessLog(LID lid, string msg, DebugInfo token, string extendMessage = null)
        {
            return WriteCoreByToken(lid, EErrorType.Process, token, null, ""); ;
        }
        //--------------------------------ParseIR----------------------------------------------
        public static LogData AddParseIRLog(LID lid, string msg, params object[] objs)
        {
            return WriteCoreByToken(lid, EErrorType.ParseIR, null, objs, msg);
        }
        public static LogData AddParseIRLog(LID lid, DebugInfo token, string msg, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            return WriteCoreByToken(lid, EErrorType.ParseIR, token, objs, msg);
        }
        //--------------------------------Runtime----------------------------------------------
        public static LogData AddRuntimeLog(LID lid, string msg, params object[] objs)
        {
            return WriteCoreByToken(lid, EErrorType.Runtime, null, objs, msg);
        }
        public static LogData AddRuntimeLog(LID lid, DebugInfo token, string msg, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            return WriteCoreByToken(lid, EErrorType.Runtime, token, objs, msg);
        }
        //--------------------------------Other----------------------------------------------
        public static LogData AddOtherLog(LID lid, string msg, params object[] objs)
        {
            return WriteCoreByToken(lid, EErrorType.Other, null, objs, msg);
        }
        public static LogData AddOtherLog(LID lid, DebugInfo token, string msg, params object[] objs)
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);
            return WriteCoreByToken(lid, EErrorType.Other, token, objs, msg);
        }
        private static LogData WriteCoreByToken(
            LID lid,
            EErrorType errorType,
            DebugInfo token,
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

            bool isAssert = errorDefine.LogType == LogType.Assert;
            bool isError = errorDefine.LogType == LogType.Error;

            if(isAssert && LogManager.Options.EnableAssertFeature )
            {
                ld.enableAssert = true;
            }
            if (LogManager.Options.BlockOnError && errorDefine.EnableAssert )
            {
                bool shouldBlockCurrent = errorDefine.BlockOnErrorAssert
                    || (isAssert && LogManager.Options.BlockOnAssert)
                    || ( LogManager.Options.BlockOnError);

                bool shouldAbortCompilation = errorDefine.AbortCompilation
                    || (isAssert && LogManager.Options.AbortCompilationOnAssert)
                    || ( LogManager.Options.AbortCompilationOnError);

                if (shouldBlockCurrent || shouldAbortCompilation)
                {
                    ld.enableAssert = true;
                }
            }
            if (token != null)
            {
                ld.filePath = token.path;
                ld.sourceBeginLine = token.beginLine;
                ld.sourceBeginChar = token.beginChar;
                ld.sourceEndLine = token.endLine;
                ld.sourceEndChar = token.endChar;
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
            
            return ld;
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
            Console.WriteLine($"[VMLog] OutputPath: {VmLogFilePath}");
        }
    }
}