using System;

namespace SimpleLanguage.source.Logging
{
    public class CompilationAbortException : Exception
    {
        public int ErrorId { get; }
        public CompilationAbortException(int id, string message) : base(message)
        {
            ErrorId = id;
        }
    }
}
