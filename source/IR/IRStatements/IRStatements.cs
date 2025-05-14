//****************************************************************************
//  File:      IRStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class MetaIRStatements
    {
        public IRMethod irMethod { get; protected set; } = null;
        public List<IRBase> irStatements => m_IRStatements;

        protected List<IRBase> m_IRStatements = new List<IRBase>();
    }
}
