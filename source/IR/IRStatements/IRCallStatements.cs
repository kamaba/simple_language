//****************************************************************************
//  File:      IRCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.IR;
using SimpleLanguage.Core.Statements;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class IRCallStatements : IRStatements
    {
        IRMetaCallLink m_IRMc = null;
        public IRCallStatements( IRMethod _iRMethod)
        {
            irMethod = _iRMethod;
        }
        public void ParseIRStatements(MetaCallStatements ms)
        {
            m_IRMc = new IRMetaCallLink();
            m_IRMc.ParseToIRDataList(irMethod, ms.metaCallLink.callNodeList);
            m_IRStatements.AddRange(m_IRMc.irList);
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("call");
            sb.Append(m_IRMc?.ToIRString());
            return sb.ToString();
        }
    }
}
