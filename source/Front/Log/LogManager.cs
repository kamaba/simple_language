using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace SimpleLanguage.Logging
{
    public sealed class LogRuntimeOptions
    {
        public bool EnableAssertFeature { get; set; } = true;
        public bool BlockOnAssert { get; set; } = true;
        public bool BlockOnError { get; set; } = false;
        public bool AbortCompilationOnAssert { get; set; } = true;
        public bool AbortCompilationOnError { get; set; } = false;
    }

    public class LogManager
    {
        private static readonly ConcurrentDictionary<LogModule, ModuleLogger> _loggers = new ConcurrentDictionary<LogModule, ModuleLogger>();
        private static readonly ConcurrentQueue<Diagnostic> _diagnostics = new ConcurrentQueue<Diagnostic>();
        private static LogRuntimeOptions _options = new LogRuntimeOptions();
        private static int s_DebugListenerAttached = 0;

        static LogManager()
        {
            EnsureBuiltinDefinitions();
            AttachDebugTraceBridge();
        }

        public static LogRuntimeOptions Options => _options;

        public static void Configure(LogRuntimeOptions options)
        {
            if (options != null)
            {
                _options = options;
            }
        }

        public static ModuleLogger GetLogger(LogModule module)
        {
            return _loggers.GetOrAdd(module, m => new ModuleLogger(m));
        }

        public static void Initialize(string csvPath)
        {
            ErrorRegistry.Instance.LoadFromCsv(csvPath);
            EnsureBuiltinDefinitions();
            AttachDebugTraceBridge();
        }

        private static void EnsureBuiltinDefinitions()
        {
            if (!ErrorRegistry.Instance.TryGet((int)LID.Unknown, out _))
            {
                ErrorRegistry.Instance.Register(new ErrorDefinition()
                {
                    Id = (int)LID.Unknown,
                    //Module = LogModule.Project,
                    LogType = LogType.Error,
                    EnableAssert = true,
                    BlockOnErrorAssert = false,
                    AbortCompilation = false,
                    DisplayType = ErrorDisplayType.Direct,
                    ParamCount = 1,
                    MessageTemplate = "{0}",
                    FixHint = "查看调用栈并在对应模块补充明确的错误码定义。",
                });
            }
        }

        private static void AttachDebugTraceBridge()
        {
            if (System.Threading.Interlocked.Exchange(ref s_DebugListenerAttached, 1) == 1)
            {
                return;
            }

            var listener = new DebugLogTraceListener();
            bool exists = false;
            foreach (TraceListener item in Trace.Listeners)
            {
                if (item is DebugLogTraceListener)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                Trace.Listeners.Add(listener);
            }
        }

        internal static void AddDiagnostic(Diagnostic diag)
        {
            if (diag != null)
            {
                _diagnostics.Enqueue(diag);
            }
        }

        public static Diagnostic[] GetDiagnosticsSnapshot()
        {
            return _diagnostics.ToArray();
        }
    }

    public class ModuleLogger : ILogger
    {
        private readonly LogModule _module;
        public ModuleLogger(LogModule module)
        {
            _module = module;
        }

        public void Log(int errorId, params object[] args)
        {
            if (!ErrorRegistry.Instance.TryGet(errorId, out var def))
            {
                Console.WriteLine($"Unknown error id:{errorId}");
                return;
            }
            var msg = FormatMessage(def, args);
            var diag = new Diagnostic()
            {
                Id = def.Id,
                LogType = def.LogType,
                //Module = def.Module,
                Message = msg,
                FixHint = def.FixHint
            };
            Console.WriteLine(diag.ToString());
            LogManager.AddDiagnostic(diag);

            HandleBlocking(def, msg);
        }

        public void LogWithToken(int errorId, object token, params object[] args)
        {
            if (!ErrorRegistry.Instance.TryGet(errorId, out var def))
            {
                Console.WriteLine($"Unknown error id:{errorId}");
                return;
            }
            var tokenInfo = ExtractTokenInfo(token);
            var msg = FormatMessage(def, args);
            var diag = new Diagnostic()
            {
                Id = def.Id,
                LogType = def.LogType,
                //Module = def.Module,
                Message = msg,
                FixHint = def.FixHint,
                FilePath = tokenInfo.Path,
                StartLine = tokenInfo.StartLine,
                StartChar = tokenInfo.StartChar,
                EndLine = tokenInfo.EndLine,
                EndChar = tokenInfo.EndChar,
                Token = token,
                TokenSummary = tokenInfo.Summary,
            };
            Console.WriteLine(diag.ToString());
            LogManager.AddDiagnostic(diag);

            HandleBlocking(def, msg);
        }

        public void Assert(int errorId, params object[] args)
        {
            if (!ErrorRegistry.Instance.TryGet(errorId, out var def))
            {
                throw new CompilationAbortException(errorId, "Unknown assert error");
            }
            if (!LogManager.Options.EnableAssertFeature || !def.EnableAssert)
            {
                Log(errorId, args);
                return;
            }
            var msg = FormatMessage(def, args);
            throw new CompilationAbortException(def.Id, msg, true);
        }

        private static string FormatMessage(ErrorDefinition def, object[] args)
        {
            string msg = def.MessageTemplate;
            try
            {
                if (def.ParamCount > 0 && args != null)
                {
                    msg = string.Format(CultureInfo.InvariantCulture, def.MessageTemplate, args);
                }
            }
            catch
            {
                msg = def.MessageTemplate + " [format error]";
            }
            return msg;
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

        private static TokenInfo ExtractTokenInfo(object token)
        {
            if (token == null)
            {
                return TokenInfo.Empty;
            }

            string path = ReadProperty<string>(token, "path") ?? string.Empty;
            int sLine = ReadProperty<int>(token, "sourceBeginLine");
            int sChar = ReadProperty<int>(token, "sourceBeginChar");
            int eLine = ReadProperty<int>(token, "sourceEndLine");
            int eChar = ReadProperty<int>(token, "sourceEndChar");
            object lexeme = ReadProperty<object>(token, "lexeme");
            object type = ReadProperty<object>(token, "type");

            string summary = string.Empty;
            if (lexeme != null || type != null)
            {
                summary = $"[Token lexeme={lexeme}, type={type}]";
            }

            return new TokenInfo(path, sLine, sChar, eLine, eChar, summary);
        }

        private static T ReadProperty<T>(object obj, string name)
        {
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null) return default;
                var val = p.GetValue(obj);
                if (val == null) return default;

                if (val is T matched)
                {
                    return matched;
                }
                return (T)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return default;
            }
        }

        private readonly struct TokenInfo
        {
            public static TokenInfo Empty => new TokenInfo(string.Empty, 0, 0, 0, 0, string.Empty);

            public TokenInfo(string path, int sLine, int sChar, int eLine, int eChar, string summary)
            {
                Path = path;
                StartLine = sLine;
                StartChar = sChar;
                EndLine = eLine;
                EndChar = eChar;
                Summary = summary;
            }

            public string Path { get; }
            public int StartLine { get; }
            public int StartChar { get; }
            public int EndLine { get; }
            public int EndChar { get; }
            public string Summary { get; }
        }
    }
}
