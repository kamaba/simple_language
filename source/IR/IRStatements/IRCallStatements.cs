//****************************************************************************
//  File:      IRCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaCallStatements
    {
        public override void ParseIRStatements()
        {
            m_MetaCallLink.ParseToIRDataList(irMethod);
            m_IRStatements.AddRange(m_MetaCallLink.irList);
        }
        public override string ToIRString()
        {
            return m_MetaCallLink.ToIRString();
        }
    }
}
