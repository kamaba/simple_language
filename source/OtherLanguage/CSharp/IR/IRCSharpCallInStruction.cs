using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core.CSharp.IR
{
    public partial class IRCallInStruction
    {
        System.Object target = null;
        System.Object[] paramObjs = new System.Object[2];
        System.Object returnObj = null;
        MethodInfo mt = null;
        
        public void Execute()
        {
            returnObj = mt.Invoke(target, paramObjs);
        }
    }
}
