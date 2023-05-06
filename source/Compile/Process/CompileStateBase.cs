using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.source.Compile.Process
{
    public enum ELevelInfo
    {
        None = 0,
        Info,
        Warning,
        Error,
    }
    public class CompileStateBase
    {
        public bool isInterrupt => m_IsInterrupt;

        protected bool m_IsInterrupt = false;

        public DateTime dateTime = new DateTime();

    }
}
