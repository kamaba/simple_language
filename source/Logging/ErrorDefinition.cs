using System;
using System.Collections.Generic;

namespace SimpleLanguage.Compile.Logging
{
    /// <summary>
    /// Severity level for an error/diagnostic.
    /// </summary>
    public enum ErrorSeverity
    {
        Assert,
        Error,
        Warning,
        Info,
        Trace
    }

    /// <summary>
    /// Logical module where an error may originate.
    /// Used to categorize diagnostics and to obtain a per-module logger.
    /// </summary>
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

    /// <summary>
    /// How a diagnostic message should be displayed to the user.
    /// </summary>
    public enum ErrorDisplayType
    {
        TokenDisplay,
        Direct,
        Fixed
    }

    /// <summary>
    /// Definition of a single error/diagnostic loaded from configuration (CSV).
    /// This contains the message template, severity, module and behavior flags
    /// such as whether the occurrence should abort the current module or later stages.
    /// </summary>
    public class ErrorDefinition
    {
        /// <summary>
        /// Numerical unique error identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Message template used with string.Format().
        /// </summary>
        public string MessageTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Severity of this error.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

        /// <summary>
        /// Expected number of parameters for the message template.
        /// </summary>
        public int ParamCount { get; set; } = 0;

        /// <summary>
        /// Owning logical module for this diagnostic.
        /// </summary>
        public ErrorModule Module { get; set; } = ErrorModule.FileMeta;

        /// <summary>
        /// If true, the logger will throw a CompilationAbortException to stop the current module.
        /// </summary>
        public bool AbortCurrent { get; set; } = false;

        /// <summary>
        /// If true, the occurrence should prevent subsequent compilation phases.
        /// (This flag is intended for higher-level coordination.)
        /// </summary>
        public bool AbortLater { get; set; } = false;

        /// <summary>
        /// Display style for the message (affects formatting or token inclusion).
        /// </summary>
        public ErrorDisplayType DisplayType { get; set; } = ErrorDisplayType.Direct;

        /// <summary>
        /// Optional hint text shown to the user explaining how to fix the error.
        /// </summary>
        public string FixHint { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Id}] {Severity} {Module}: {MessageTemplate}";
        }
    }
}
