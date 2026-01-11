using System;
using SimpleLanguage.Compile;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Logging
{
    /// <summary>
    /// Represents a runtime diagnostic event created by the logging system.
    /// Diagnostic objects are structured and can be collected, formatted or exported.
    /// </summary>
    public class Diagnostic
    {
        /// <summary>
        /// Error identifier from the registry (CSV or programmatically registered).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Severity category.
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Logical module where this diagnostic originated.
        /// </summary>
        public ErrorModule Module { get; set; }

        /// <summary>
        /// The fully formatted message text ready for display.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional file path if the diagnostic is associated with a file/token.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Start line in source (1-based). Zero if not set.
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Start character in source (1-based). Zero if not set.
        /// </summary>
        public int StartChar { get; set; }

        /// <summary>
        /// End line in source (1-based). Zero if not set.
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// End character in source (1-based). Zero if not set.
        /// </summary>
        public int EndChar { get; set; }

        /// <summary>
        /// Optional human-facing guidance on how to fix the issue.
        /// </summary>
        public string FixHint { get; set; }

        /// <summary>
        /// Optional original Token associated with the diagnostic.
        /// </summary>
        public Token Token { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                return $"[{Id}] {Severity} {FilePath}({StartLine},{StartChar}) {Message}";
            }
            return $"[{Id}] {Severity} {Message}";
        }
    }
}
