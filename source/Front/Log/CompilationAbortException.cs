using System;

namespace SimpleLanguage.Logging
{
    public class CompilationAbortException : Exception
    {
        public int ErrorId { get; }
        public bool AbortCompilationProcess { get; }

        public CompilationAbortException(int id, string message, bool abortCompilationProcess = false) : base(message)
        {
            ErrorId = id;
            AbortCompilationProcess = abortCompilationProcess;
        }
    }
}
