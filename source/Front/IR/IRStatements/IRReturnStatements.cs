//****************************************************************************
//  File:      IRReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
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
        private IRExpressBase m_ReturnValueExpress = null;
        public void ParseIRStatements(MetaReturnStatements ms)
        {
            if (ms.express != null)
            {
                m_ReturnValueExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                m_IRStatements.Add(m_ReturnValueExpress);

                IRStoreVariable irsv = IRStoreVariable.CreateStaticReturnIRSV(this.irMethod, ms?.token);
                m_IRStatements.Add(irsv);

                IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, irMethod.funEndLabelData );
                m_IRStatements.Add(irbranch);
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
