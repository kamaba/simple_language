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
        public List<IRBase> irStatements => m_IRStatements;

        protected List<IRBase> m_IRStatements = new List<IRBase>();
        public virtual void ParseIRStatements()
        {         
        }
        public virtual string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("parseIR");
            sb.AppendLine("{");
            //for (int i = 0; i < m_IRDataList.Count; i++)
            //{
            //    sb.AppendLine(m_IRDataList[i].ToString());
            //}
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
