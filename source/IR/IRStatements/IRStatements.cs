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

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaStatements
    {
        public IRMethod irMethod
        {
            get { return m_OwnerMetaBlockStatements.ownerMetaFunction.irMethod; }
        }
        public List<IRData> irDataList => m_IRDataList;

        protected List<IRData> m_IRDataList = new List<IRData>();
        public virtual void ParseIRStatements()
        {         
        }
    }
}
