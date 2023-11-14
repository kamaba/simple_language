//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBlockStatements
    {
        public IRNop blockStart = null;
        public override void ParseIRStatements()
        {
            blockStart = new IRNop(irMethod);
            blockStart.data.SetDebugInfoByToken( m_FileMetaBlockSyntax.token );
            m_IRStatements.Add(blockStart);
        }
        public void ParseAllIRStatements()
        {
            blockStart = new IRNop(this.m_OwnerMetaFunction.irMethod);
            if (m_FileMetaBlockSyntax?.token  != null)
            {
                blockStart.data.SetDebugInfoByToken( m_FileMetaBlockSyntax.token );
            }
            m_IRStatements.Add(blockStart);

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                mbs.ParseIRStatements();
                m_IRStatements.AddRange(mbs.irStatements);
                mbs = mbs.nextMetaStatements;
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("#block {");

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                sb.AppendLine(mbs.ToIRString());
                mbs = mbs.nextMetaStatements;
            }
            sb.AppendLine("}#");
            return sb.ToString();
        }
    }
}
