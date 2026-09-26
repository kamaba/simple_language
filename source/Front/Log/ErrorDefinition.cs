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
    public class ErrorDefinition
    {
        public int Id { get; set; }
        public LogType LogType { get; set; } = LogType.Error;
        public bool EnableAssert { get; set; } = true;
        public bool Pass { get; set; } = false;
        public int ParamCount { get; set; } = 0;
        public string Demo { get; set; } = "";
        public string[] MessageTemplateArray { get; set; } = new string[2];
        public string[] FixedTipArray { get; set; } = new string[2];
    }
}
