//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRAssignStatements : IRStatements
    {
        public IRAssignStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        protected IRExpress m_IRExpress = null;
        protected IRStoreVariable m_StoreVariable = null;
        public void ParseIRStatements( MetaAssignStatements ms )
        {
            if (ms.finalMetaExpress != null)
            {
                m_IRExpress = new IRExpress(irMethod, ms.finalMetaExpress);
                m_IRStatements.Add(m_IRExpress);
            }

            IRMetaCallLink irmc = new IRMetaCallLink();
            irmc.ParseToIRDataList(irMethod, ms.leftMetaExpress.metaCallLink.callNodeList);
            m_IRStatements.AddRange(irmc.irList);

            var mv = ms.leftMetaExpress.GetMetaVariable();
            var vfrom = mv.variableFrom == MetaVariable.EVariableFrom.Argument ? IRMetaVariableFrom.Argument : IRMetaVariableFrom.LocalStatement;
            IRStoreVariable irsv = new IRStoreVariable(irMethod, mv.GetHashCode(), vfrom);
            m_IRStatements.Add(irsv);
        }
    }
}
