using System;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Compile.Logging
{
    public class Diagnostic
    {
        public int Id { get; set; }
        public ErrorSeverity Severity { get; set; }
        public ErrorModule Module { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public int StartLine { get; set; }
        public int StartChar { get; set; }
        public int EndLine { get; set; }
        public int EndChar { get; set; }
        public string FixHint { get; set; }
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
