using System;
using System.Diagnostics;

namespace SimpleLanguage.Logging
{
    public sealed class DebugLogTraceListener : TraceListener
    {
        private readonly ModuleLogger _logger;

        public DebugLogTraceListener()
        {
            _logger = LogManager.GetLogger(LogModule.Project);
        }

        public override void Fail(string message)
        {
            LogDebugMessage(LogType.Assert, message, null);
        }

        public override void Fail(string message, string detailMessage)
        {
            var msg = string.IsNullOrWhiteSpace(detailMessage)
                ? message
                : message + " " + detailMessage;
            LogDebugMessage(LogType.Assert, msg, null);
        }

        public override void Write(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                LogDebugMessage(LogType.Trace, message, null);
            }
        }

        public override void WriteLine(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                LogDebugMessage(LogType.Trace, message, null);
            }
        }

        private void LogDebugMessage(LogType logType, string message, object token)
        {
            const int debugBridgeId = (int)LID.Unknown;

            if (!ErrorRegistry.Instance.TryGet(debugBridgeId, out _))
            {
                ErrorRegistry.Instance.Register(new ErrorDefinition
                {
                    Id = debugBridgeId,
                    LogType = logType,
                    EnableAssert = true,
                    BlockOnErrorAssert = logType == LogType.Assert,
                    AbortCompilation = false,
                    DisplayType = ErrorDisplayType.Direct,
                    ParamCount = 1,
                    MessageTemplate = "{0}",
                    FixHint = "请根据 Debug 输出定位具体调用点。",
                });
            }

            if (token != null)
            {
                _logger.LogWithToken(debugBridgeId, token, message);
            }
            else
            {
                _logger.Log(debugBridgeId, message);
            }
        }
    }
}
