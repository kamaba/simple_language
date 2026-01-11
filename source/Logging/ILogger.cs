using System;

namespace SimpleLanguage.Compile.Logging
{
    public interface ILogger
    {
        void Log(int errorId, params object[] args);
        void LogWithToken(int errorId, Token token, params object[] args);
        void Assert(int errorId, params object[] args); // throws CompilationAbortException when configured
    }
}
