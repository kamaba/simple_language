using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SimpleLanguage.Logging
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

        public bool TryResolveByMessage(string message, out ErrorDefinition def)
        {
            def = null;
            if (string.IsNullOrWhiteSpace(message) || _dict.Count == 0)
            {
                return false;
            }

            var text = message.Trim();

            def = _dict.Values.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.MessageTemplate)
                && string.Equals(d.MessageTemplate.Trim(), text, StringComparison.Ordinal));
            if (def != null)
            {
                return true;
            }

            def = _dict.Values.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.MessageTemplate)
                && text.StartsWith(d.MessageTemplate.Trim(), StringComparison.Ordinal));
            if (def != null)
            {
                return true;
            }

            def = _dict.Values.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.MessageTemplate)
                && d.MessageTemplate.Trim().Length >= 8
                && text.Contains(d.MessageTemplate.Trim(), StringComparison.Ordinal));
            return def != null;
        }

        /// <summary>
        /// Enumerate all registered definitions ordered by id.
        /// </summary>
        public IEnumerable<ErrorDefinition> AllDefinitions => _dict.Values.OrderBy(d => d.Id);

        public void LoadFromCsv(string path)
        {
            if (!File.Exists(path)) return;
            using var sr = new StreamReader(path);
            _ = sr.ReadLine();
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = SplitCsvLine(line);
                if (parts.Length < 9) continue;
                if (!int.TryParse(parts[0], out var id)) continue;
                var def = new ErrorDefinition();
                def.Id = id;

                // preferred schema:
                // id,module,logType,enableAssert,blockOnErrorAssert,abortCompilation,messageTemplate,paramCount,fixHint
                // legacy schema fallback:
                // id,messageTemplate,severity,paramCount,module,abortCurrent,abortLater,displayType,fixHint
                if (IsLegacySchema(parts))
                {
                    def.MessageTemplate = parts[1];
                    Enum.TryParse<LogType>(parts[2], true, out var sev);
                    def.LogType = sev;
                    if (!int.TryParse(parts[3], out var pc)) pc = 0;
                    def.ParamCount = pc;
                    Enum.TryParse<LogModule>(parts[4], true, out var modLegacy);
                    def.Module = modLegacy;
                    bool.TryParse(parts[5], out var ac);
                    def.BlockOnErrorAssert = ac;
                    bool.TryParse(parts[6], out var al);
                    def.AbortCompilation = al;
                    Enum.TryParse<ErrorDisplayType>(parts[7], true, out var dtLegacy);
                    def.DisplayType = dtLegacy;
                    def.FixHint = parts[8];
                }
                else
                {
                    Enum.TryParse<LogModule>(parts[1], true, out var mod);
                    def.Module = mod;
                    Enum.TryParse<LogType>(parts[2], true, out var lt);
                    def.LogType = lt;
                    bool.TryParse(parts[3], out var enableAssert);
                    def.EnableAssert = enableAssert;
                    bool.TryParse(parts[4], out var blockOnErrorAssert);
                    def.BlockOnErrorAssert = blockOnErrorAssert;
                    bool.TryParse(parts[5], out var abortCompilation);
                    def.AbortCompilation = abortCompilation;
                    def.MessageTemplate = parts[6];
                    if (!int.TryParse(parts[7], out var paramCount)) paramCount = 0;
                    def.ParamCount = paramCount;
                    def.FixHint = parts[8];
                    def.DisplayType = ErrorDisplayType.TokenDisplay;
                }
                _dict[def.Id] = def;
            }
        }

        private static bool IsLegacySchema(string[] parts)
        {
            return parts.Length >= 2
                && (parts[1].Contains("{")
                    || parts[1].Contains(" ")
                    || parts[1].Contains("Error", StringComparison.OrdinalIgnoreCase)
                    || parts[1].Contains("Warning", StringComparison.OrdinalIgnoreCase));
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
        public void Register(ErrorDefinition def)
        {
            _dict[def.Id] = def;
        }
    }
}
