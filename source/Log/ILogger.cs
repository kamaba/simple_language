
using System;

namespace SimpleLanguage.Logging
{
    public interface ILogger
    {
        void Log(int errorId, params object[] args);
        void LogWithToken(int errorId, object token, params object[] args);
        void Assert(int errorId, params object[] args);
    }
}
