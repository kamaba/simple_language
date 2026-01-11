using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SimpleLanguage.Compile.Logging
{
    /// <summary>
    /// Registry of ErrorDefinition loaded from a CSV configuration.
    /// Provides lookup by numeric id and an enumerable view of all definitions.
    /// </summary>
    public class ErrorRegistry
    {
        private ConcurrentDictionary<int, ErrorDefinition> _dict = new ConcurrentDictionary<int, ErrorDefinition>();

        /// <summary>
        /// Global singleton instance. The registry is lightweight and safe to use from multiple threads.
        /// </summary>
        public static ErrorRegistry Instance { get; } = new ErrorRegistry();

        private ErrorRegistry() { }

        /// <summary>
        /// Try get an ErrorDefinition by its id.
        /// </summary>
        public bool TryGet(int id, out ErrorDefinition def)
        {
            return _dict.TryGetValue(id, out def);
        }

        /// <summary>
        /// Enumerate all registered definitions ordered by id.
        /// </summary>
        public IEnumerable<ErrorDefinition> AllDefinitions => _dict.Values.OrderBy(d => d.Id);

        /// <summary>
        /// Load definitions from a CSV file. The expected columns are:
        /// id,messageTemplate,severity,paramCount,module,abortCurrent,abortLater,displayType,fixHint
        /// Lines with invalid format are skipped.
        /// </summary>
        /// <param name="path">Path to CSV file.</param>
        public void LoadFromCsv(string path)
        {
            if (!File.Exists(path)) return;
            using var sr = new StreamReader(path);
            string header = sr.ReadLine();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = SplitCsvLine(line);
                // id, messageTemplate, severity, paramCount, module, abortCurrent, abortLater, displayType, fixHint
                if (parts.Length < 9) continue;
                if (!int.TryParse(parts[0], out var id)) continue;
                var def = new ErrorDefinition();
                def.Id = id;
                def.MessageTemplate = parts[1];
                Enum.TryParse<ErrorSeverity>(parts[2], true, out var sev);
                def.Severity = sev;
                if (!int.TryParse(parts[3], out var pc)) pc = 0;
                def.ParamCount = pc;
                Enum.TryParse<ErrorModule>(parts[4], true, out var mod);
                def.Module = mod;
                bool.TryParse(parts[5], out var ac);
                def.AbortCurrent = ac;
                bool.TryParse(parts[6], out var al);
                def.AbortLater = al;
                Enum.TryParse<ErrorDisplayType>(parts[7], true, out var dt);
                def.DisplayType = dt;
                def.FixHint = parts[8];
                _dict[def.Id] = def;
            }
        }

        /// <summary>
        /// Basic CSV parser that supports quoted fields. It does not handle escaped quotes.
        /// It is intentionally simple because the CSV we use is small and controlled.
        /// </summary>
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
        public void Register(ErrorDefinition def)
        {
            _dict[def.Id] = def;
        }
    }
}
