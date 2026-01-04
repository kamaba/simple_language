using System;
using System.Collections.Generic;

namespace SimpleLanguage.Compile.Logging
{
    public enum ErrorSeverity
    {
        Assert,
        Error,
        Warning,
        Info,
        Trace
    }
    public enum ErrorModule
    {
        TokenParse,
        Node,
        FileMeta,
        CoreMeta,
        IR,
        IROutput,
        VM,
        Project
    }
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
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        public int ParamCount { get; set; } = 0;
        public ErrorModule Module { get; set; } = ErrorModule.FileMeta;
        public bool AbortCurrent { get; set; } = false;
        public bool AbortLater { get; set; } = false;
        public ErrorDisplayType DisplayType { get; set; } = ErrorDisplayType.Direct;
        public string FixHint { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Id}] {Severity} {Module}: {MessageTemplate}";
        }
    }
}
