//****************************************************************************
//  File:      IRCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.IR;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class IRCallStatements : IRStatements
    {
        private List<IRBase> m_IRList = new List<IRBase>();

        public IRCallStatements( IRMethod _iRMethod)
        {
            irMethod = _iRMethod;
        }
        public void ParseIRStatements(MetaCallStatements ms)
        {
            IRMetaCallLink irmc = new IRMetaCallLink();
            irmc.ParseToIRDataList(irMethod, ms.metaCallLink.callNodeList);
            m_IRStatements.AddRange(irmc.irList);
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("call");
            //sb.Append(base.ToIRString());
            return sb.ToString();
        }
        //public override string ToIRString()
        //{
        //    return m_MetaCallLink.ToIRString();
        //}
    }
}
