using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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
        private static LogRuntimeOptions _options = new LogRuntimeOptions();
        private static int s_DebugListenerAttached = 0;
        private static ConcurrentDictionary<int, ErrorDefinition> _dict = new ConcurrentDictionary<int, ErrorDefinition>();
        public static int LanguageIndex { get; set; } = 0;

        static LogManager()
        {
            EnsureBuiltinDefinitions();
            //AttachDebugTraceBridge();
        }

        public static LogRuntimeOptions Options => _options;

        public static void Configure(LogRuntimeOptions options)
        {
            if (options != null)
            {
                _options = options;
            }
        }
        public static void Initialize(string csvPath)
        {
            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                LoadFromCsv(csvPath);
            }
            else
            {
                TryLoadEmbeddedCsv();
            }
            EnsureBuiltinDefinitions();
            //AttachDebugTraceBridge();
        }

        private static void TryLoadEmbeddedCsv()
        {
            string[] candidates =
            {
                "ErrorDefinitions.csv",
            };

            var asms = new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetEntryAssembly(),
                Assembly.GetCallingAssembly(),
            }
            .Where(a => a != null)
            .Distinct()
            .ToArray();

            foreach (var asm in asms)
            {
                var names = asm.GetManifestResourceNames();
                foreach (var resName in names)
                {
                    if (!candidates.Any(c => resName.EndsWith(c, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream == null)
                    {
                        continue;
                    }

                    LoadFromCsvStream(stream);
                    return;
                }
            }
        }
        /// <summary>
        /// Try get an ErrorDefinition by its id.
        /// </summary>
        public static bool TryGet(int id, out ErrorDefinition def)
        {
            return _dict.TryGetValue(id, out def);
        }

        public static void LoadFromCsv(string path)
        {
            if (!File.Exists(path)) return;
            using var sr = new StreamReader(path);
            LoadFromCsvReader(sr);
        }
        public static void LoadFromCsvStream(Stream stream)
        {
            if (stream == null) return;
            using var sr = new StreamReader(stream);
            LoadFromCsvReader(sr);
        }

        private static void LoadFromCsvReader(StreamReader sr)
        {
            _ = sr.ReadLine();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = SplitCsvLine(line);
                if (parts.Length < 8) continue;
                if (!int.TryParse(parts[0], out var id)) continue;
                var def = new ErrorDefinition();
                def.Id = id;


                Enum.TryParse<LogType>(parts[1], true, out var sev);
                def.LogType = sev;


                bool.TryParse(parts[2], out var ac);
                def.EnableAssert = ac;
                bool.TryParse(parts[3], out var al);
                def.Pass = al;
                if (!int.TryParse(parts[4], out var pc)) pc = 0;
                def.ParamCount = pc;

                def.Demo = parts[5];

                def.MessageTemplateArray[0] = parts[6];
                def.FixedTipArray[0] = parts[7];

                def.MessageTemplateArray[1] = parts[8];
                def.FixedTipArray[1] = parts[9];


                _dict[def.Id] = def;
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            bool inQuote = false;
            var cur = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }
                if (c == ',' && !inQuote)
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                    continue;
                }
                cur.Append(c);
            }
            list.Add(cur.ToString());
            return list.ToArray();
        }

        /// <summary>
        /// Register or update an ErrorDefinition programmatically.
        /// </summary>
        public static void Register(ErrorDefinition def)
        {
            _dict[def.Id] = def;
        }

        private static void EnsureBuiltinDefinitions()
        {
            // Unknown fallback has been removed; all logs should use explicit LIDs.
        }

        //private static void AttachDebugTraceBridge()
        //{
        //    if (System.Threading.Interlocked.Exchange(ref s_DebugListenerAttached, 1) == 1)
        //    {
        //        return;
        //    }

        //    var listener = new DebugLogTraceListener();
        //    bool exists = false;
        //    foreach (TraceListener item in Trace.Listeners)
        //    {
        //        if (item is DebugLogTraceListener)
        //        {
        //            exists = true;
        //            break;
        //        }
        //    }
        //    if (!exists)
        //    {
        //        Trace.Listeners.Add(listener);
        //    }
        //}

        //internal static void AddDiagnostic(Diagnostic diag)
        //{
        //    if (diag != null)
        //    {
        //        _diagnostics.Enqueue(diag);
        //    }
        //}

        //public static Diagnostic[] GetDiagnosticsSnapshot()
        //{
        //    return _diagnostics.ToArray();
        //}
    }

}
