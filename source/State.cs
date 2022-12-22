using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class StateBase
    {
        public bool isInterrupt => m_IsInterrupt;

        protected bool m_IsInterrupt = false;
    }
}
