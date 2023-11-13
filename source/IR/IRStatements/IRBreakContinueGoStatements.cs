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

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBreakStatements
    {
        public IRBranch irBrach = null;
        public override void ParseIRStatements()
        {
            irBrach = new IRBranch(irMethod,  EIROpCode.Br, null );
            m_IRStatements.Add(irBrach);
            if (m_FileMetaKeyOnlySyntax.token != null )
            {
                irBrach.data.SetDebugInfoByToken( m_FileMetaKeyOnlySyntax.token );
            }
            irMethod.AddLabelDict(irBrach.data);
            if (m_ForStatements != null )
            {
                irBrach.data.opValue = m_ForStatements.endIRData.data;
            }
            else if( m_WhileStatements != null )
            {
                irBrach.data.opValue = m_WhileStatements.endData;
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#break#");

            sb.AppendLine(base.ToIRString());

            return sb.ToString();
        }
    }
    public partial class MetaContinueStatements
    {
        public IRBranch irBrach = null;
        public override void ParseIRStatements()
        {
            irBrach = new IRBranch(irMethod, EIROpCode.Br, null );
            if (m_FileMetaKeyOnlySyntax.token != null)
            {
                irBrach.SetDebugInfoByToken( m_FileMetaKeyOnlySyntax.token );
            }
            irMethod.AddLabelDict(irBrach.data);
            m_IRStatements.Add(irBrach);
            if (m_ForStatements != null)
            {
                irBrach.data.opValue = m_ForStatements.forStartIRData;
            }
            else if (m_WhileStatements != null)
            {
                irBrach.data.opValue = m_WhileStatements.endData;
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#continue#");

            sb.AppendLine(base.ToIRString());

            return sb.ToString();
        }
    }
    public partial class MetaGotoLabelStatements
    {
        public IRLabel labelIR = null;
        public override void ParseIRStatements()
        {
            labelIR = new IRLabel(irMethod, labelData.label, isLabel);
            m_IRStatements.Add(labelIR);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(isLabel?"#labal ":"#goto " + m_FileMetaKeyGotoLabelSyntax?.token.ToString() + "#");

            sb.AppendLine(base.ToIRString());

            return sb.ToString();
        }
    }
}