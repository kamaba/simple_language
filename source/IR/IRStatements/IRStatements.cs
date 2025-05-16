//****************************************************************************
//  File:      IRStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRStatements
    {
        public IRMethod irMethod { get; protected set; } = null;
        public List<IRBase> irStatements => m_IRStatements;

        protected List<IRBase> m_IRStatements = new List<IRBase>();
    }
}
