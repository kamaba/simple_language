//****************************************************************************
//  File:      IRBreakContinueGoStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class MetaIRBreakStatements : MetaIRStatements
    {
        public IRBranch irBrach = null;
        public void ParseIRStatements(MetaBreakStatements ms)
        {
            irBrach = new IRBranch(irMethod,  EIROpCode.Br, null );
            m_IRStatements.Add(irBrach);
            //if (m_FileMetaKeyOnlySyntax.token != null )
            //{
            //    irBrach.data.SetDebugInfoByToken( m_FileMetaKeyOnlySyntax.token );
            //}
            //if ( m_ForStatements != null )
            //{
            //    irBrach.data.opValue = m_ForStatements.endIRData.data;
            //}
            //else if( m_WhileStatements != null )
            //{
            //    irBrach.data.opValue = m_WhileStatements.endIRData.data;
            //}
        }
    }
    public class MetaIRContinueStatements : MetaIRStatements
    {
        public IRBranch irBrach = null;
        public void ParseIRStatements(MetaContinueStatements mcs )
        {
            irBrach = new IRBranch(irMethod, EIROpCode.Br, null );
            //if (m_FileMetaKeyOnlySyntax.token != null)
            //{
            //    irBrach.SetDebugInfoByToken( m_FileMetaKeyOnlySyntax.token );
            //}
            m_IRStatements.Add(irBrach);
            //if (mcs.m_ForStatements != null)
            //{
            //    irBrach.data.opValue = m_ForStatements.forStartIRData.data;
            //}
            //else if ( m_WhileStatements != null)
            //{
            //     irBrach.data.opValue = m_WhileStatements.whileStartIRData.data;
            //}
        }
    }
    public class MetaIRGotoLabelStatements : MetaIRStatements
    {
        public IRLabel labelIR = null;
        public void ParseIRStatements(MetaGotoLabelStatements mgls )
        {
            labelIR = new IRLabel(irMethod, mgls.labelData.label, mgls.isLabel);
            m_IRStatements.Add(labelIR);
        }
    }
}