//****************************************************************************
//  File:      Log.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            Project,    //初始化工程
            Process,
            ParseToken,    //识别token
            ParseNode,     //识别node
            ParseFile,         //构建FileMeta文件
            ParseMeta,             //构建元数据
            GenIR,                  //生成IR
            VM,                     //使用VM
        }

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

        //public Dictionary<EMetaType, MetaBase> valDict = new Dictionary<EMetaType, MetaBase>();

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
            if (!string.IsNullOrWhiteSpace(extendMessage))
            {
                m_SB.Append($" Extend: [" + extendMessage + " ]");
            }

            return m_SB.ToString();
        }
    }
    public class Log
    {
        static ConcurrentQueue<LogData> logDataList = new ConcurrentQueue<LogData>();
        private static readonly object s_VmLogFileLock = new object();

        /// <summary>
        /// VM 日志落盘路径：优先 <c>SIMPLELANG_EXPORT_OUTDIR/vm.txt</c>，否则当前工作目录下的 <c>vm.txt</c>。
        /// </summary>
        public static string GetVmLogFilePath()
        {
            var outDir = Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (!string.IsNullOrWhiteSpace(outDir))
            {
                try
                {
                    Directory.CreateDirectory(outDir);
                    return Path.Combine(outDir, "vm.txt");
                }
                catch
                {
                    /* fall through */
                }
            }
            return Path.Combine(Environment.CurrentDirectory, "vm.txt");
        }

        static void AppendVmLogToFile(LogData data)
        {
            var path = GetVmLogFilePath();
            lock (s_VmLogFileLock)
            {
                try
                {
                    File.AppendAllText(path, data.ToString() + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    /* 避免日志写盘失败影响 VM 执行 */
                }
            }
        }

        public static void AddCodeFileLog( LogData data )
        {
            logDataList.Enqueue(data);
            if (data.errorType == LogData.EErrorType.VM)
                AppendVmLogToFile(data);
        }

        public static LogData Add(LID lid, params object[] args)
        {
            return WriteCore(lid, LogData.EErrorType.Project, null, null, null, args);
        }

        public static LogData Add(LID lid, string extendMessage, params object[] args)
        {
            return WriteCore(lid, LogData.EErrorType.Project, null, extendMessage, null, args);
        }

        public static LogData Add(LID lid, object token, params object[] args)
        {
            return WriteCore(lid, LogData.EErrorType.Project, token, null, null, args);
        }

        public static LogData Add(LID lid, object token, string extendMessage, params object[] args)
        {
            return WriteCore(lid, LogData.EErrorType.Project, token, extendMessage, null, args);
        }
        //public static LogData AddInInitProject(Token token, EError err, string msg)
        //{
        //    LogData ld = new LogData()
        //    {
        //        filePath = token.path,
        //        sourceBeginLine = token.sourceBeginLine,
        //        sourceEndLine = token.sourceEndLine,
        //        errorType = LogData.EErrorType.InitProject
        //    };
        //    ld.message = msg;
        //    ld.error = err;
        //    AddCodeFileLog(ld);
        //    return ld;
        //}
        public static LogData AddProcessLog(EProcess proc, LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.Process, null, null, msg, null);
        }

        public static LogData AddTokenLog(LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.ParseToken, null, null, msg, null);
        }

        public static LogData AddTokenLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.ParseToken, token, extendMessage, msg, null);
        }

        public static LogData AddNodeLog(LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.ParseNode, null, null, msg, null);
        }

        public static LogData AddNodeLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.ParseNode, token, extendMessage, msg, null);
        }

        public static LogData AddFileMetaLog(LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.ParseFile, null, null, msg, null);
        }

        public static LogData AddFileMetaLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.ParseFile, token, extendMessage, msg, null);
        }

        public static LogData AddMetaCoreLog(LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, null);
        }
        public static LogData AddMetaCoreLog(LID lid, List<Token> tokens, params object[] objs )
        {
            //return WriteCore(lid, LogData.EErrorType.ParseMeta, null, null, msg, args);

            return null;
        }

        public static LogData AddMetaCoreLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.ParseMeta, token, extendMessage, msg, null);
        }

        public static LogData AddIRLog(LID lid, string msg)
        {
            return WriteCore(lid, LogData.EErrorType.GenIR, null, null, msg, null);
        }

        public static LogData AddIRLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.GenIR, token, extendMessage, msg, null);
        }

        public static LogData AddProjectLog(LID lid, string msg, params object[] par )
        {
            return WriteCore(lid, LogData.EErrorType.Project, null, null, msg, null);
        }

        public static LogData AddProjectLog(LID lid, string msg, object token, string extendMessage = null)
        {
            return WriteCore(lid, LogData.EErrorType.Project, token, extendMessage, msg, null);
        }

        public static LogData AddInHandleToken(string path, int sbl, int sel, LID lid, string msg)
        {
            var ld = AddTokenLog(lid, msg);
            ld.filePath = path;
            ld.sourceBeginLine = sbl;
            ld.sourceBeginChar = sel;
            return ld;
        }
        public static LogData AddVM( LID lid, string msg )
        {
            return WriteCore(lid, LogData.EErrorType.VM, null, null, msg, null);
        }
        private static LogData WriteCore(
            LID lid,
            LogData.EErrorType errorType,
            object token,
            string extendMessage,
            string explicitMessage,
            object[] args)
        {
            var tokenData = ParseTokenData(token);
            var resolvedLid = ResolveLid(lid, explicitMessage, args);

            var ld = new LogData()
            {
                errorType = errorType,
                time = DateTime.Now
            };
            ld.filePath = tokenData.Path;
            ld.sourceBeginLine = tokenData.SourceBeginLine;
            ld.sourceBeginChar = tokenData.SourceBeginChar;
            ld.sourceEndLine = tokenData.SourceEndLine;
            ld.sourceEndChar = tokenData.SourceEndChar;
            ld.error = resolvedLid;

            var finalMessage = BuildMessage(resolvedLid, explicitMessage, args);
            if (!string.IsNullOrWhiteSpace(extendMessage))
            {
                finalMessage = string.IsNullOrWhiteSpace(finalMessage)
                    ? extendMessage
                    : finalMessage + " | " + extendMessage;
            }
            ld.message = finalMessage;
            ld.extendMessage = extendMessage;

            AddCodeFileLog(ld);

            ForwardToUnifiedLogger((int)resolvedLid, errorType, token, finalMessage, args);
            return ld;
        }

        private static LID ResolveLid(LID lid, string explicitMessage, object[] args)
        {
            if (lid != LID.Unknown)
            {
                return lid;
            }

            string candidate = explicitMessage;
            if (string.IsNullOrWhiteSpace(candidate) && args != null && args.Length > 0)
            {
                candidate = args[0]?.ToString();
            }

            if (!string.IsNullOrWhiteSpace(candidate)
                && ErrorRegistry.Instance.TryResolveByMessage(candidate, out var def))
            {
                return (LID)def.Id;
            }

            return LID.Unknown;
        }

        private static string BuildMessage(LID lid, string explicitMessage, object[] args)
        {
            if (!string.IsNullOrWhiteSpace(explicitMessage))
            {
                return explicitMessage;
            }

            if (ErrorRegistry.Instance.TryGet((int)lid, out var def))
            {
                try
                {
                    if (def.ParamCount > 0 && args != null && args.Length > 0)
                    {
                        return string.Format(System.Globalization.CultureInfo.InvariantCulture, def.MessageTemplate, args);
                    }
                    return def.MessageTemplate;
                }
                catch
                {
                    return def.MessageTemplate;
                }
            }

            if (args != null && args.Length > 0)
            {
                return args[0]?.ToString() ?? lid.ToString();
            }
            return lid.ToString();
        }

        private static void ForwardToUnifiedLogger(int defId, LogData.EErrorType errorType, object token, string finalMessage, object[] args)
        {
            try
            {
                var module = GetModuleByErrorType(errorType);
                if (!ErrorRegistry.Instance.TryGet(defId, out _))
                {
                    ErrorRegistry.Instance.Register(new ErrorDefinition()
                    {
                        Id = defId,
                        MessageTemplate = "{0}",
                        LogType = LogType.Warning,
                        ParamCount = 1,
                        Module = module,
                        EnableAssert = true,
                        BlockOnErrorAssert = false,
                        AbortCompilation = false,
                        DisplayType = token != null ? ErrorDisplayType.TokenDisplay : ErrorDisplayType.Direct,
                        FixHint = ""
                    });
                }

                var logger = LogManager.GetLogger(module);
                if (token != null)
                {
                    logger.LogWithToken(defId, token, finalMessage);
                }
                else if (args != null && args.Length > 0)
                {
                    logger.Log(defId, args);
                }
                else
                {
                    logger.Log(defId, finalMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log forward failed: " + ex.Message);
            }
        }

        private static LogModule GetModuleByErrorType(LogData.EErrorType errorType)
        {
            return errorType switch
            {
                LogData.EErrorType.ParseToken => LogModule.TokenParse,
                LogData.EErrorType.ParseNode => LogModule.NodeParse,
                LogData.EErrorType.ParseFile => LogModule.FileMeta,
                LogData.EErrorType.ParseMeta => LogModule.CoreMeta,
                LogData.EErrorType.GenIR => LogModule.IROutput,
                LogData.EErrorType.Project => LogModule.Project,
                LogData.EErrorType.Process => LogModule.Project,
                LogData.EErrorType.VM => LogModule.VM,
                _ => LogModule.Project,
            };
        }

        public static void PrintLog()
        {
            Console.WriteLine("----------错误收集 开始---------------------");

            foreach ( var  ld in logDataList.ToArray() )
            {
                Console.WriteLine(ld.ToString());
            }
            Console.WriteLine("----------错误收集 结束---------------------");
        }

        private static ParsedTokenData ParseTokenData(object token)
        {
            if (token == null)
            {
                return ParsedTokenData.Empty;
            }

            return new ParsedTokenData(
                ReadTokenProp<string>(token, "path") ?? string.Empty,
                ReadTokenProp<int>(token, "sourceBeginLine"),
                ReadTokenProp<int>(token, "sourceBeginChar"),
                ReadTokenProp<int>(token, "sourceEndLine"),
                ReadTokenProp<int>(token, "sourceEndChar"));
        }

        private static T ReadTokenProp<T>(object token, string propName)
        {
            try
            {
                var prop = token.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null) return default;
                var value = prop.GetValue(token);
                if (value == null) return default;
                if (value is T typed) return typed;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        private readonly struct ParsedTokenData
        {
            public static ParsedTokenData Empty => new ParsedTokenData(string.Empty, 0, 0, 0, 0);

            public ParsedTokenData(string path, int sourceBeginLine, int sourceBeginChar, int sourceEndLine, int sourceEndChar)
            {
                Path = path;
                SourceBeginLine = sourceBeginLine;
                SourceBeginChar = sourceBeginChar;
                SourceEndLine = sourceEndLine;
                SourceEndChar = sourceEndChar;
            }

            public string Path { get; }
            public int SourceBeginLine { get; }
            public int SourceBeginChar { get; }
            public int SourceEndLine { get; }
            public int SourceEndChar { get; }
        }
    }
}