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
            }
            // 裸 ret（void 函数）也必须生成跳转到函数结束，
            // 否则块内的提前返回会退化为顺序执行后续语句。
            IRBranch irbranch = new IRBranch(this.irMethod, EIROpCode.BrLabel, irMethod.funEndLabelData );
            m_IRStatements.Add(irbranch);
        }
    }

    public class MetaIRTRStatements
    {
        public void ParseIRStatements()
        {
        }
    }
}
