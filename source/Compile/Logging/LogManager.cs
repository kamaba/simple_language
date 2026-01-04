using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Compile.Logging
{
    public class LogManager
    {
        private static readonly ConcurrentDictionary<ErrorModule, ModuleLogger> _loggers = new ConcurrentDictionary<ErrorModule, ModuleLogger>();
        public static ModuleLogger GetLogger(ErrorModule module)
        {
            return _loggers.GetOrAdd(module, m => new ModuleLogger(m));
        }

        public static void Initialize(string csvPath)
        {
            ErrorRegistry.Instance.LoadFromCsv(csvPath);
        }
    }

    public class ModuleLogger : ILogger
    {
        private readonly ErrorModule _module;
        public ModuleLogger(ErrorModule module)
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
            var msg = FormatMessage(def, null, args);
            var diag = new Diagnostic()
            {
                Id = def.Id,
                Severity = def.Severity,
                Module = def.Module,
                Message = msg,
                FixHint = def.FixHint
            };
            Console.WriteLine(diag.ToString());

            if (def.AbortCurrent || def.Severity == ErrorSeverity.Assert)
            {
                throw new CompilationAbortException(def.Id, msg);
            }
        }

        public void LogWithToken(int errorId, Token token, params object[] args)
        {
            if (!ErrorRegistry.Instance.TryGet(errorId, out var def))
            {
                Console.WriteLine($"Unknown error id:{errorId}");
                return;
            }
            var msg = FormatMessage(def, token, args);
            var diag = new Diagnostic()
            {
                Id = def.Id,
                Severity = def.Severity,
                Module = def.Module,
                Message = msg,
                FixHint = def.FixHint,
                FilePath = token?.path,
                StartLine = token?.sourceBeginLine ?? 0,
                StartChar = token?.sourceBeginChar ?? 0,
                EndLine = token?.sourceEndLine ?? 0,
                EndChar = token?.sourceEndChar ?? 0,
                Token = token
            };
            Console.WriteLine(diag.ToString());

            if (def.AbortCurrent || def.Severity == ErrorSeverity.Assert)
            {
                throw new CompilationAbortException(def.Id, msg);
            }
        }

        public void Assert(int errorId, params object[] args)
        {
            if (!ErrorRegistry.Instance.TryGet(errorId, out var def))
            {
                throw new CompilationAbortException(errorId, "Unknown assert error");
            }
            var msg = FormatMessage(def, null, args);
            throw new CompilationAbortException(def.Id, msg);
        }

        private static string FormatMessage(ErrorDefinition def, Token token, object[] args)
        {
            string msg = def.MessageTemplate;
            try
            {
                if (def.ParamCount > 0 && args != null)
                {
                    msg = string.Format(CultureInfo.InvariantCulture, def.MessageTemplate, args);
                }
                if (def.DisplayType == ErrorDisplayType.TokenDisplay && token != null)
                {
                    msg = token.ToLexemeAllString() + " " + msg;
                }
            }
            catch
            {
                // formatting fallback
                msg = def.MessageTemplate + " [format error]";
            }
            return msg;
        }
    }
}
