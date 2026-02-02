using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeVariable
    {
        public RuntimeDefType runtimeDefType => m_RuntimeDefType;

        private RuntimeDefType m_RuntimeDefType = null;
        public RuntimeVariable() { }
    }
}
