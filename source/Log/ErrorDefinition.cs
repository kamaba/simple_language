using System;
using System.Collections.Generic;

namespace SimpleLanguage.Logging
{
    public enum LogType
    {
        Assert,
        Error,
        Warning,
        Info,
        Trace,
    }

    public enum LogModule
    {
        // legacy aliases
        TokenParse = 0,
        NodeParse = 1,
        FileMeta = 2,
        CoreMeta = 3,
        IROutput = 4,
        Project = 5,
        VM = 6,
    }

    /// <summary>
    /// How a diagnostic message should be displayed to the user.
    /// </summary>
    public enum ErrorDisplayType
    {
        TokenDisplay,
        Direct,
        Fixed
    }

    public class ErrorDefinition
    {
        public int Id { get; set; }
        public string MessageTemplate { get; set; } = string.Empty;
        public LogType LogType { get; set; } = LogType.Error;
        public int ParamCount { get; set; } = 0;
        public LogModule Module { get; set; } = LogModule.FileMeta;
        public bool EnableAssert { get; set; } = true;
        public bool BlockOnErrorAssert { get; set; } = false;
        public bool AbortCompilation { get; set; } = false;
        public ErrorDisplayType DisplayType { get; set; } = ErrorDisplayType.Direct;
        public string FixHint { get; set; } = string.Empty;

        // compatibility bridge
        public LogType Severity { get => LogType; set => LogType = value; }
        public bool AbortCurrent { get => BlockOnErrorAssert; set => BlockOnErrorAssert = value; }
        public bool AbortLater { get => AbortCompilation; set => AbortCompilation = value; }

        public override string ToString()
        {
            return $"[{Id}] {LogType} {Module}: {MessageTemplate}";
        }
    }
}
