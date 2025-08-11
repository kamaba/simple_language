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
using static SimpleLanguage.Core.MetaVariable;

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

            var clist = ms.leftMetaExpress.metaCallLink.callNodeList;
            for (int i = 0; i < clist.Count; i++)
            {
                if (i < clist.Count - 1)
                {
                    var list = IRMetaCallLink.ExecOnceCnode(this.irMethod, clist[i]);
                    m_IRStatements.AddRange(list);
                }
                else
                {
                    var mv = clist[i].GetRetMetaVariable();
                    var irsmv = IRStoreVariable.CreateIRStoreVariable(this.irMethod, mv);
                    m_IRStatements.Add(irsmv);
                }
            }
        }
    }
}
