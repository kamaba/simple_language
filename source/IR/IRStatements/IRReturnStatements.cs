//****************************************************************************
//  File:      IRReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRReturnStatements : IRStatements
    {
        public IRReturnStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        private IRExpress m_ReturnValueExpress = null;
        public void ParseIRStatements(MetaReturnStatements ms)
        {
            if (ms.express != null)
            {
                m_ReturnValueExpress = new IRExpress(this.irMethod, ms.express );
                m_IRStatements.Add(m_ReturnValueExpress);

                IRStoreVariable irsv = IRStoreVariable.CreateStaticReturnIRSV();
                m_IRStatements.Add(irsv);
            }
        }
    }

    public class MetaIRTRStatements
    {
        public void ParseIRStatements()
        {
        }
    }
}
